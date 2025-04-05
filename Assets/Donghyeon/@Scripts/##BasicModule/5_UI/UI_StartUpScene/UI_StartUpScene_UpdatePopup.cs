using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Assets.Scripts.UI;

namespace Unity.Assets.Scripts.UI
{
    public class UpdatePopup : UI_Popup
    {
        enum Texts
        {
            TitleText,
            MessageText
        }

        enum Buttons
        {
            OkButton,
            CancelButton
        }

        private System.Action _onOkCallback;
        private System.Action _onCancelCallback;

        public override bool Init()
        {
            if (base.Init() == false)
                return false;

            BindTexts(typeof(Texts));
            BindButtons(typeof(Buttons));

            GetButton((int)Buttons.OkButton).gameObject.BindEvent((PointerEventData data) => OnClickOk());
            GetButton((int)Buttons.CancelButton).gameObject.BindEvent((PointerEventData data) => OnClickCancel());

            return true;
        }

        public void SetInfo(string title, string message, System.Action onOk, System.Action onCancel)
        {
            if (!_init)
                Init();

            GetText((int)Texts.TitleText).text = title;
            GetText((int)Texts.MessageText).text = message;
            _onOkCallback = onOk;
            _onCancelCallback = onCancel;
        }

        private void OnClickOk()
        {
            _onOkCallback?.Invoke();
            ClosePopupUI();
        }

        private void OnClickCancel()
        {
            _onCancelCallback?.Invoke();
            ClosePopupUI();
        }
    }
} 