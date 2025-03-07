﻿using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 表情制御の一番か二番目くらいに偉いやつ。VRMBlendShapeProxy.Applyをする権利を保有する。
    /// </summary>
    public class FaceControlManager : MonoBehaviour
    {
        private static readonly BlendShapeKey BlinkLKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R);

        //NOTE: まばたき自体は3種類どれかが排他で適用される。複数走っている場合、external > image > autoの優先度で適用する。
        [SerializeField] private ExternalTrackerBlink externalTrackerBlink = null;
        [SerializeField] private ImageBasedBlinkController imageBasedBlinkController = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;
        
        [SerializeField] private EyeJitter randomEyeJitter = null;
        [SerializeField] private ExternalTrackerEyeJitter externalTrackEyeJitter = null;
        
        private bool _hasModel = false;
        private FaceControlConfiguration _config;
        //直接使うわけじゃないけどGCされてほしくないので、念の為参照を保持してる
        private FaceControlManagerMessageIo _messageIo;

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, IMessageReceiver receiver, IMessageSender sender, 
            FaceControlConfiguration config, EyeBonePostProcess eyeBonePostProcess)
        {
            _config = config;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            
            var _ = new FaceControlConfigurationReceiver(receiver, config);
            _messageIo = new FaceControlManagerMessageIo(receiver, sender, eyeBonePostProcess, this);
        }
        
        public DefaultFunBlendShapeModifier DefaultBlendShape { get; } 
            = new DefaultFunBlendShapeModifier();

        /// <summary> WebCamベースのトラッキング中でも自動まばたきを優先するかどうかを取得、設定します。 </summary>
        public bool PreferAutoBlinkOnWebCamTracking { get; set; } = true;
        
        public void Accumulate(VRMBlendShapeProxy proxy, float weight = 1f)
        {
            if (!_hasModel)
            {
                return;
            }
            
            //NOTE: ここのデフォルトfunだが
            //「パーフェクトシンク使用中」「FaceSwitch適用中」「Word to Motion適用中」
            //の3ケースでは適用されると困る。
            //で、ここに書いておくと上記3ケースではそもそもAccumulateが呼ばれないため、うまく動く。
            DefaultBlendShape.Apply(proxy);
            
            var blinkSource =
                _config.ControlMode == FaceControlModes.ExternalTracker ? externalTrackerBlink.BlinkSource :
                (_config.ControlMode == FaceControlModes.WebCam && !PreferAutoBlinkOnWebCamTracking) ? imageBasedBlinkController.BlinkSource :
                autoBlink.BlinkSource;
            
            proxy.AccumulateValue(BlinkLKey, blinkSource.Left * weight);
            proxy.AccumulateValue(BlinkRKey, blinkSource.Right * weight);
        }

        private void Update()
        {
            //眼球運動はモード別で切り替える。外部トラッキング中はホンモノのJitterが使えるから使えばいいじゃん、という話
            bool canUseExternalEyeJitter =
                _config.ControlMode == FaceControlModes.ExternalTracker && externalTrackEyeJitter.IsTracked;
            randomEyeJitter.IsActive = !canUseExternalEyeJitter;
            externalTrackEyeJitter.IsActive = canUseExternalEyeJitter;
        }
                
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
        }
    }
    
    
    /// <summary> まばたき状態の値を提供します。 </summary>
    public interface IBlinkSource
    {
        float Left { get; }
        float Right { get; }
    }

    /// <summary> 単なるプロパティで<see cref="IBlinkSource"/>を実装します。 </summary>
    public class RecordBlinkSource : IBlinkSource
    {
        public float Left { get; set; }
        public float Right { get; set; }
    }
}
