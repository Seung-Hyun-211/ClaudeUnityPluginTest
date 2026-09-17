using UnityEngine;
using UnityEngine.UI;
using Game.Player;

namespace Game.UI
{
    /// <summary>Generic fill bar bound to any stat, reused for hunger and thirst.</summary>
    public class StatBarUIView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private IReadOnlyStat stat;

        public void Bind(IReadOnlyStat targetStat)
        {
            if (stat != null)
            {
                stat.Changed -= Refresh;
            }

            stat = targetStat;
            stat.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (stat != null)
            {
                stat.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            fillImage.fillAmount = stat.Max <= 0f ? 0f : stat.Current / stat.Max;
        }
    }
}
