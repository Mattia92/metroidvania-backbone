using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUIController : MonoBehaviour {
    private GameObject[] healthContainerList;
    private Image[] healthFillList;
    public GameObject healthContainerPrefab;
    public Transform healthParent;

    // Start is called before the first frame update
    void Start() {
        healthContainerList = new GameObject[PlayerController.Instance.maxHealth];
        healthFillList = new Image[PlayerController.Instance.maxHealth];

        PlayerController.Instance.onHealthChangedCallback += UpdateHealthHUD;
        InstantiateHealthContainers();
        UpdateHealthHUD();
    }

    // Update is called once per frame
    void Update() {

    }

    private void UpdateHealthHUD() {
        SetHealthContainers();
        SetFilledHealth();
    }

    private void InstantiateHealthContainers() {
        for (int i = 0; i < PlayerController.Instance.maxHealth; i++) {
            GameObject healthContainer = Instantiate(healthContainerPrefab);
            healthContainer.transform.SetParent(healthParent, false);
            healthContainerList[i] = healthContainer;
            healthFillList[i] = healthContainer.transform.Find("Health Fill").GetComponent<Image>();
        }
    }

    private void SetHealthContainers() {
        for (int i = 0; i < healthContainerList.Length; i++) {
            Debug.Log(i);
            Debug.Log(PlayerController.Instance.maxHealth);
            Debug.Log(healthContainerList.Length);
            Debug.Log(healthContainerList[i]);
            healthContainerList[i].SetActive(i < PlayerController.Instance.maxHealth);
        }
    }

    private void SetFilledHealth() {
        for (int i = 0; i < healthFillList.Length; i++) {
            healthFillList[i].fillAmount = i < PlayerController.Instance.health ? 1 : 0;
        }
    }
}
