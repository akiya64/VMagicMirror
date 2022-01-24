using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> マウスボタンイベントをグローバルフックから拾ってUIスレッドで発火してくれる凄いやつだよ </summary>
    /// <remarks>
    /// 以前はここでキーボードのグローバルフックも拾っていたが、RawInput実装に切り替わったため廃止した。
    /// IReleaseBeforeQuitを消してもいいのだが、グローバルフックに関係ある立場なので一応残している
    /// </remarks>
    public class GlobalHookInputChecker : MonoBehaviour, IReleaseBeforeQuit, IKeyMouseEventSource
    {        
        private static readonly Dictionary<int, string> MouseEventNumberToEventName = new Dictionary<int, string>()
        {
            [WindowsAPI.MouseMessages.WM_LBUTTONDOWN] = "LDown",
            [WindowsAPI.MouseMessages.WM_LBUTTONUP] = "LUp",
            [WindowsAPI.MouseMessages.WM_RBUTTONDOWN] = "RDown",
            [WindowsAPI.MouseMessages.WM_RBUTTONUP] = "RUp",
            [WindowsAPI.MouseMessages.WM_MBUTTONDOWN] = "MDown",
            [WindowsAPI.MouseMessages.WM_MBUTTONUP] = "MUp",
        };
        
        //NOTE: このクラスはキーボード関連のイベントは何も出さない
        public IObservable<string> PressedRawKeys { get; } = Observable.Empty<string>();
        public IObservable<string> KeyDown { get; } = Observable.Empty<string>();
        public IObservable<string> KeyUp { get; } = Observable.Empty<string>();     
        
        public IObservable<string> MouseButton => _mouseButton;
        private readonly Subject<string> _mouseButton = new Subject<string>();
        private readonly ConcurrentQueue<string> _mouseButtonConcurrent = new ConcurrentQueue<string>();

        private MouseHook _mouseHook = null;
        private readonly MessageLoopThread _thread = new MessageLoopThread();

        // WPFの入力で代替したい場合ここのコメントアウトを解除
        // [Inject]
        // public void Initialize(IMessageReceiver receiver)
        // {
        // receiver.AssignCommandHandler(
        //     VmmCommands.MouseButton,
        //     c => _mouseButton.OnNext(c.Content)
        //     );
        // }

        public void ReleaseBeforeCloseConfig()
        {
            //何もしない
        }

        public Task ReleaseResources() => Task.CompletedTask;

        private void OnMouseButtonEvent(int wParamVal)
        {
            _mouseButtonConcurrent.Enqueue(MouseEventNumberToEventName[wParamVal]);
        }
        
        private void Start()
        {
            _thread.Run(
                () =>
                {
                    _mouseHook = new MouseHook();
                    _mouseHook.MouseButton += OnMouseButtonEvent;
                    _mouseHook.Start();
                },
                () =>
                {
                    _mouseHook.MouseButton -= OnMouseButtonEvent;
                    _mouseHook.Dispose();
                });
        }

        private void Update()
        {
            while (_mouseButtonConcurrent.TryDequeue(out var info))
            {
                _mouseButton.OnNext(info);
            }            
        }

        private void OnDestroy()
        {
            _thread.Stop();
        }
    }
}
