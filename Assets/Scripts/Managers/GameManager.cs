using UnityEngine;

namespace ProductsPlease.Managers
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        public static GameManager Instance;

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        #endregion

        public DaysManager DaysManager { get; private set; }
        public BenefitsManager BenefitsManager { get; private set; }
        public UIManager UIManager { get; private set; }

        private void Init()
        {
            DaysManager = GetComponent<DaysManager>();
            BenefitsManager = GetComponent<BenefitsManager>();
            UIManager = GetComponent<UIManager>();
        }
    }
}