using UnityEngine;
using UnityEngine.UI;

using Game.Building;

namespace Game.UI
{
    [RequireComponent(typeof(Image))]
    public class PlayerObjectUI : GameObjectUI
    {
        [Tooltip("이 UI가 표시될 상호작용 종류")]
        [SerializeField] private LoadTypes _loadType;
        [SerializeField] private Image _interactionImage;

        public LoadTypes LoadType => _loadType;

        protected override void Awake()
        {
            base.Awake();

            _interactionImage = GetComponent<Image>();
            if (_interactionImage == null)
            {
                Debug.LogError($"Interaction Image is missing in {gameObject.name}");
                enabled = false;
                return;
            }

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _interactionImage.enabled = visible;
        }
    }
}
