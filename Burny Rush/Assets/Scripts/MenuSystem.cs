using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MenuSystem : MonoBehaviour
{
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text loadingPersentage;

    public void LoadGame(int sceneIndex){
        StartCoroutine(LoadAsynchronously(sceneIndex));
    }

    IEnumerator LoadAsynchronously(int sceneIndex){
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        while(!operation.isDone){
            float progress = Mathf.Clamp01(operation.progress/0.9f);
            loadingSlider.value = progress;
            loadingPersentage.text = (progress * 100f)+"%" ;
            Debug.Log(progress);

            yield return null;
        }
    }

    public void ExitGame(){
        Debug.Log("Exiting Game!");
        Application.Quit();
    }
}
