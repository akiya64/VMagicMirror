﻿using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    //「顔・表情」タブのViewModel
    public class FaceSettingViewModel : SettingViewModelBase
    {
        public FaceSettingViewModel() : this(
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<InstallPathChecker>(),
            ModelResolver.Instance.Resolve<DeviceListSource>(),
            ModelResolver.Instance.Resolve<MicrophoneStatus>()
            )
        {
        }

        internal FaceSettingViewModel(
            MotionSettingModel model,
            InstallPathChecker installPathChecker,
            DeviceListSource deviceListSource,
            MicrophoneStatus microphoneStatus
            )
        {
            _model = model;
            _deviceListSource = deviceListSource;
            _microphoneStatus = microphoneStatus;

            ResetFaceBasicSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(ResetFaceBasicSetting)
                );
            ResetFaceEyeSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetFaceEyeSetting)
                );
            ResetFaceBlendShapeSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetFaceBlendShapeSetting)
                );

            CalibrateFaceCommand = new ActionCommand(() => model.RequestCalibrateFace());

            if (!IsInDegignMode)
            {
                return;
            }

            ShowInstallPathWarning = installPathChecker.HasMultiByteCharInInstallPath;
            _model.EyeBoneRotationScale.AddWeakEventHandler(OnEyeBoneRotationScaleChanged);
            _model.FaceNeutralClip.AddWeakEventHandler(OnFaceClipChanged);
            _model.FaceOffsetClip.AddWeakEventHandler(OnFaceClipChanged);
            UpdateEyeRotRangeText();
        }

        private readonly MotionSettingModel _model;
        private readonly DeviceListSource _deviceListSource;
        private readonly MicrophoneStatus _microphoneStatus;
        private readonly FaceMotionBlendShapeNameStore _blendShapeNameStore = new FaceMotionBlendShapeNameStore();

        private void OnEyeBoneRotationScaleChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateEyeRotRangeText();
        }

        private void OnFaceClipChanged(object? sender, PropertyChangedEventArgs e)
        {
            _blendShapeNameStore.Refresh(_model.FaceNeutralClip.Value, _model.FaceOffsetClip.Value);
        }

        #region Face

        public RProperty<bool> EnableFaceTracking => _model.EnableFaceTracking;

        public bool ShowInstallPathWarning { get; }

        public RProperty<bool> AutoBlinkDuringFaceTracking => _model.AutoBlinkDuringFaceTracking;
        public RProperty<bool> EnableBodyLeanZ => _model.EnableBodyLeanZ;
        public RProperty<bool> EnableBlinkAdjust => _model.EnableBlinkAdjust;
        public RProperty<bool> EnableVoiceBasedMotion => _model.EnableVoiceBasedMotion;
        public RProperty<bool> DisableFaceTrackingHorizontalFlip => _model.DisableFaceTrackingHorizontalFlip;
        public RProperty<bool> EnableWebCamHighPowerMode => _model.EnableWebCamHighPowerMode;

        public RProperty<string> CameraDeviceName => _model.CameraDeviceName;
        public ReadOnlyObservableCollection<string> CameraNames => _deviceListSource.CameraNames;

        public ActionCommand CalibrateFaceCommand { get; }

        public RProperty<string> CalibrateFaceData => _model.CalibrateFaceData;
        public RProperty<int> FaceDefaultFun => _model.FaceDefaultFun;

        public ReadOnlyObservableCollection<string> BlendShapeNames => _blendShapeNameStore.BlendShapeNames;

        public RProperty<string> FaceNeutralClip => _model.FaceNeutralClip;
        public RProperty<string> FaceOffsetClip => _model.FaceOffsetClip;

        #endregion

        #region Eye

        public RProperty<bool> UseLookAtPointNone => _model.UseLookAtPointNone;
        public RProperty<bool> UseLookAtPointMousePointer => _model.UseLookAtPointMousePointer;
        public RProperty<bool> UseLookAtPointMainCamera => _model.UseLookAtPointMainCamera;
        public RProperty<int> EyeBoneRotationScale => _model.EyeBoneRotationScale;

        //NOTE: ちょっと作法が悪いけど、「-7.0 ~ +7.0」のようなテキストでViewにわたす
        private const double EyeRotDefaultRange = 7.0;
        private string _eyeRotRangeText = $"-{EyeRotDefaultRange:0.00} ~ +{EyeRotDefaultRange:0.00}";
        public string EyeRotRangeText
        {
            get => _eyeRotRangeText;
            private set => SetValue(ref _eyeRotRangeText, value);
        }
        private void UpdateEyeRotRangeText()
        {
            double range = EyeRotDefaultRange * EyeBoneRotationScale.Value * 0.01;
            EyeRotRangeText = $"-{range:0.00} ~ +{range:0.00}";
        }

        #endregion

        #region Mouth

        public RProperty<bool> EnableLipSync => _model.EnableLipSync;
        public RProperty<string> LipSyncMicrophoneDeviceName => _model.LipSyncMicrophoneDeviceName;
        public RProperty<int> MicrophoneSensitivity => _model.MicrophoneSensitivity;

        public RProperty<bool> ShowMicrophoneVolume => _microphoneStatus.ShowMicrophoneVolume;
        public RProperty<int> MicrophoneVolumeValue => _microphoneStatus.MicrophoneVolumeValue;

        public ReadOnlyObservableCollection<string> MicrophoneNames => _deviceListSource.MicrophoneNames;

        #endregion

        #region Reset API

        public ActionCommand ResetFaceBasicSettingCommand { get; }
        public ActionCommand ResetFaceEyeSettingCommand { get; }
        public ActionCommand ResetFaceBlendShapeSettingCommand { get; }

        private void ResetFaceBasicSetting()
        {
            _model.ResetFaceBasicSetting();
            //NOTE: 保存されない値だけど一応やる
            ShowMicrophoneVolume.Value = false;
        }

        #endregion
    }
}
