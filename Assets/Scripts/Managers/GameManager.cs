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


        public int correctScansThisDay = 0;
        public int incorrectScansThisDay = 0;
        private void Init()
        {
            DaysManager = GetComponent<DaysManager>();
            BenefitsManager = GetComponent<BenefitsManager>();
            UIManager = GetComponent<UIManager>();
        }
    }
}