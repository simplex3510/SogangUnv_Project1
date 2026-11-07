using UnityEngine;

namespace Game.Interaction
{
    /// <summary>상호작용 존 안에 있을 때만 표시되고, 항상 카메라를 바라보는 월드 스페이스 프롬프트.</summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private GameObject _prompt;
        [SerializeField] private Camera _camera;

        private int _zonesInside;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            SetVisible(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<IInteractable>() == null) return;

            _zonesInside++;
            SetVisible(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<IInteractable>() == null) return;

            _zonesInside = Mathf.Max(0, _zonesInside - 1);
            if (_zonesInside == 0) SetVisible(false);
        }

        private void LateUpdate()
        {
            if (_prompt.activeSelf && _camera != null)
                _prompt.transform.rotation = _camera.transform.rotation;
        }

        private void SetVisible(bool visible) => _prompt.SetActive(visible);
    }
}
