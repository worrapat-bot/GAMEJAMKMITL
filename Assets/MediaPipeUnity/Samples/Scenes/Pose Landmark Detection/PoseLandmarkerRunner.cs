// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.Rendering;
using System.ComponentModel; // 🚨 ต้องใช้สำหรับ Description ในคลาส PoseLandmarkerDetectionConfig ที่รวมเข้ามา

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
    {
        // Events สำหรับส่งสัญญาณการยกไหล่
        public static event System.Action OnShoulderLiftDetected;
        public static event System.Action OnShoulderDropDetected;
        // Events สำหรับการยกมือ
        public static event System.Action OnLeftHandRaised;
        public static event System.Action OnRightHandRaised;

        [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;

        private Experimental.TextureFramePool _textureFramePool;

        public readonly PoseLandmarkerDetectionConfig config = new PoseLandmarkerDetectionConfig();

        private float _initialLeftShoulderY = float.NaN;
        private float _initialRightShoulderY = float.NaN;
        private const float NORMALIZED_LIFT_THRESHOLD = 0.005f;
        // สถานะควบคุม: ยิง Event ไปแล้วหรือยังในการยกไหล่ครั้งนี้
        private bool _isShoulderLiftEventFired = false;

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        protected override IEnumerator Run()
        {
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Image Read Mode = {config.ImageReadMode}");
            Debug.Log($"Model = {config.ModelName}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumPoses = {config.NumPoses}");
            Debug.Log($"MinPoseDetectionConfidence = {config.MinPoseDetectionConfidence}");
            Debug.Log($"MinPosePresenceConfidence = {config.MinPosePresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
            Debug.Log($"OutputSegmentationMasks = {config.OutputSegmentationMasks}");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetPoseLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);
            taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
                yield break;
            }

            // Use RGBA32 as the input format.
            // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
            _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

            // NOTE: The screen will be resized later, keeping the aspect ratio.
            screen.Initialize(imageSource);

            SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
            _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally;
            var flipVertically = transformationOptions.flipVertically;

            // Always setting rotationDegrees to 0 to avoid the issue that the detection becomes unstable when the input image is rotated.
            // https://github.com/homuler/MediaPipeUnityPlugin/issues/1196
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            AsyncGPUReadbackRequest req = default;
            var waitUntilReqDone = new WaitUntil(() => req.done);
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

            // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                // Build the input Image
                Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU:
                        if (!canUseGpuImage)
                        {
                            throw new System.Exception("ImageReadMode.GPU is not supported");
                        }
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildGPUImage(glContext);
                        // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
                        // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
                        yield return waitForEndOfFrame;
                        break;
                    case ImageReadMode.CPU:
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                    case ImageReadMode.CPUAsync:
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        yield return waitUntilReqDone;

                        if (req.hasError)
                        {
                            Debug.LogWarning($"Failed to read texture from the image source");
                            continue;
                        }
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                        {
                            ProcessPoseActions(result);
                            _poseLandmarkerResultAnnotationController.DrawNow(result);
                        }
                        else
                        {
                            _poseLandmarkerResultAnnotationController.DrawNow(default);
                        }
                        DisposeAllMasks(result);
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                        {
                            ProcessPoseActions(result);
                            _poseLandmarkerResultAnnotationController.DrawNow(result);
                        }
                        else
                        {
                            _poseLandmarkerResultAnnotationController.DrawNow(default);
                        }
                        DisposeAllMasks(result);
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
        {
            ProcessPoseActions(result);

            _poseLandmarkerResultAnnotationController.DrawLater(result);
            DisposeAllMasks(result);
        }

        private void ProcessPoseActions(PoseLandmarkerResult result)
        {
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                return;
            }

            var allLandmarksContainer = result.poseLandmarks[0];
            var landmarks = allLandmarksContainer.landmarks;

            // ตรวจสอบว่ามี Landmark ถึง Index 16 (ข้อมือขวา) หรือไม่
            if (landmarks == null || landmarks.Count < 16)
            {
                return;
            }

            const int LEFT_SHOULDER_INDEX = 11;
            const int RIGHT_SHOULDER_INDEX = 12;
            const int LEFT_WRIST_INDEX = 15;
            const int RIGHT_WRIST_INDEX = 16;

            const float HAND_RAISE_THRESHOLD_Y = 0.05f;

            float currentLeftY = landmarks[LEFT_SHOULDER_INDEX].y;
            float currentRightY = landmarks[RIGHT_SHOULDER_INDEX].y;
            float currentLeftWristY = landmarks[LEFT_WRIST_INDEX].y;
            float currentRightWristY = landmarks[RIGHT_WRIST_INDEX].y;

            // --- 1. Logic ตรวจจับการยกไหล่ (สำหรับต้นไม้) ---

            if (float.IsNaN(_initialLeftShoulderY))
            {
                _initialLeftShoulderY = currentLeftY;
                _initialRightShoulderY = currentRightY;
                Debug.Log("Calibration: Initial shoulder positions set.");
                return;
            }

            float leftLiftDelta = _initialLeftShoulderY - currentLeftY;
            float rightLiftDelta = _initialRightShoulderY - currentRightY;

            if (leftLiftDelta >= NORMALIZED_LIFT_THRESHOLD || rightLiftDelta >= NORMALIZED_LIFT_THRESHOLD)
            {
                if (!_isShoulderLiftEventFired)
                {
                    Debug.Log("🎉 SHOULDER LIFT DETECTED! (Count +1)");
                    OnShoulderLiftDetected?.Invoke();
                    _isShoulderLiftEventFired = true;
                }
            }
            else if (leftLiftDelta < NORMALIZED_LIFT_THRESHOLD / 2 && rightLiftDelta < NORMALIZED_LIFT_THRESHOLD / 2)
            {
                if (_isShoulderLiftEventFired)
                {
                    Debug.Log("Shoulder dropped. Ready for next lift.");
                    OnShoulderDropDetected?.Invoke();
                    _isShoulderLiftEventFired = false;
                }
            }

            // --- 2. Logic ตรวจจับการยกมือ ---

            // ตรวจสอบมือซ้าย: ถ้าข้อมือซ้ายสูงกว่าไหล่ซ้าย
            if (currentLeftWristY < currentLeftY - HAND_RAISE_THRESHOLD_Y)
            {
                Debug.Log("✋ Left Hand Raised!");
                OnLeftHandRaised?.Invoke();
            }

            // ตรวจสอบมือขวา: ถ้าข้อมือขวาสูงกว่าไหล่ขวา
            if (currentRightWristY < currentRightY - HAND_RAISE_THRESHOLD_Y)
            {
                Debug.Log("✋ Right Hand Raised!");
                OnRightHandRaised?.Invoke();
            }
        }

        private void DisposeAllMasks(PoseLandmarkerResult result)
        {
            if (result.segmentationMasks != null)
            {
                foreach (var mask in result.segmentationMasks)
                {
                    mask.Dispose();
                }
            }
        }

        // 🚨 [รวมคลาส] ส่วนของ ModelType และ PoseLandmarkerDetectionConfig (เพื่อแก้ Error CS0246)

        public enum ModelType : int
        {
            [Description("Pose landmarker (lite)")]
            BlazePoseLite = 0,
            [Description("Pose landmarker (Full)")]
            BlazePoseFull = 1,
            [Description("Pose landmarker (Heavy)")]
            BlazePoseHeavy = 2,
        }

        public class PoseLandmarkerDetectionConfig
        {
            public Tasks.Core.BaseOptions.Delegate Delegate { get; set; } =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
          Tasks.Core.BaseOptions.Delegate.CPU;
#else
            Tasks.Core.BaseOptions.Delegate.GPU;
#endif

            public ImageReadMode ImageReadMode { get; set; } = ImageReadMode.CPUAsync;

            public ModelType Model { get; set; } = ModelType.BlazePoseFull;
            public Tasks.Vision.Core.RunningMode RunningMode { get; set; } = Tasks.Vision.Core.RunningMode.LIVE_STREAM;

            public int NumPoses { get; set; } = 1;
            public float MinPoseDetectionConfidence { get; set; } = 0.5f;
            public float MinPosePresenceConfidence { get; set; } = 0.5f;
            public float MinTrackingConfidence { get; set; } = 0.5f;
            public bool OutputSegmentationMasks { get; set; } = false;
            public string ModelName => Model.GetDescription() ?? Model.ToString();
            public string ModelPath
            {
                get
                {
                    switch (Model)
                    {
                        case ModelType.BlazePoseLite:
                            return "pose_landmarker_lite.bytes";
                        case ModelType.BlazePoseFull:
                            return "pose_landmarker_full.bytes";
                        case ModelType.BlazePoseHeavy:
                            return "pose_landmarker_heavy.bytes";
                        default:
                            return null;
                    }
                }
            }

            public PoseLandmarkerOptions GetPoseLandmarkerOptions(PoseLandmarkerOptions.ResultCallback resultCallback = null)
            {
                return new PoseLandmarkerOptions(
                    new Tasks.Core.BaseOptions(Delegate, modelAssetPath: ModelPath),
                    runningMode: RunningMode,
                    numPoses: NumPoses,
                    minPoseDetectionConfidence: MinPoseDetectionConfidence,
                    minPosePresenceConfidence: MinPosePresenceConfidence,
                    minTrackingConfidence: MinTrackingConfidence,
                    outputSegmentationMasks: OutputSegmentationMasks,
                    resultCallback: resultCallback
                );
            }
        }
    }
}