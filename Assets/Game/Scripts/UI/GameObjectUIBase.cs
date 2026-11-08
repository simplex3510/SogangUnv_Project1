using UnityEngine;

namespace Game.UI
{
    public class GameObjectUI : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _rectTransform;

        protected virtual void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError($"Main Camera is missing in {gameObject.name}");
                enabled = false;
            }

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                Debug.LogError($"RectTransform is missing in {gameObject.name}");
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            _rectTransform.transform.rotation = _camera.transform.rotation; 
        }
    }
}