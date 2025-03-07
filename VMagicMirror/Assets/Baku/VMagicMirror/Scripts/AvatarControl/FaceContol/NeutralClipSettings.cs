﻿using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// デフォルト表情関連の設定を保持して適用するクラスです。
    /// とくにPresetのNeutralクリップについて、実態(VMCとかがやってる)に即して任意で有効化できるようにするのが狙いです。
    /// </summary>
    public class NeutralClipSettings : MonoBehaviour
    {
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.FaceNeutralClip,
                c =>
                {
                    HasValidNeutralClipKey = !string.IsNullOrWhiteSpace(c.Content);
                    if (HasValidNeutralClipKey)
                    {
                        NeutralClipKey = BlendShapeKeyFactory.CreateFrom(c.Content);
                    }
                });

            receiver.AssignCommandHandler(
                VmmCommands.FaceOffsetClip,
                c =>
                {
                    HasValidOffsetClipKey = !string.IsNullOrWhiteSpace(c.Content);
                    if (HasValidOffsetClipKey)
                    {
                        OffsetClipKey = BlendShapeKeyFactory.CreateFrom(c.Content);
                    }
                });
        }
        
        public void ApplyNeutralClip(VRMBlendShapeProxy proxy, float weight = 1f) 
        {
            if (HasValidNeutralClipKey)
            {
                proxy.AccumulateValue(NeutralClipKey, weight);
            }
        }

        public void ApplyOffsetClip(VRMBlendShapeProxy proxy, float weight = 1f)
        {
            if (HasValidOffsetClipKey)
            {
                proxy.AccumulateValue(OffsetClipKey, weight);
            }
        }

        private bool HasValidNeutralClipKey = false;
        private BlendShapeKey NeutralClipKey;

        private bool HasValidOffsetClipKey = false;
        private BlendShapeKey OffsetClipKey;
        
    }
}
