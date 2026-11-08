using UnityEngine;

using Game.Building;
using Game.UI;

namespace Game.Player.Interaction
{
    /// <summary>상호작용 종류에 맞는 프롬프트만 켜고 나머지는 끈다.</summary>
    public class PlayerUIController : MonoBehaviour
    {
        [SerializeField] private PlayerObjectUI[] _interactionUIs;

        private LoadTypes _shownType = LoadTypes.None;

        public void Show(LoadTypes loadType)
        {
            if (_shownType == loadType)
            {
                return;
            }

            _shownType = loadType;

            foreach (PlayerObjectUI ui in _interactionUIs)
            {
                ui.SetVisible(ui.LoadType == loadType);
            }
        }
    }
}
