using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        InGame,
        Paused
    }

    public GameState currentState { get; private set; }

    /// <summary>
    ///  References to important scene objects, set automatically on scene load.
    /// </summary>
    private SceneRefs refs;
    /// <summary>
    ///  Reference to the ObjectAnimator singleton for handling UI animations.
    /// </summary>
    private ObjectAnimator objAnim;
    private bool hasBegun = false;
    private bool gamePaused = false;

    private void Start()
    {
        currentState = GameState.MainMenu;
        objAnim = ObjectAnimator.instance;
        BindSceneRefs();
        ApplyState(currentState);
    }


    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        ApplyState(currentState);
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        BindSceneRefs();
        ApplyState(currentState);
    }

    /// <summary>
    /// Finds and binds the SceneRefs component in the scene, including inactive objects.
    /// <br/>For future reference, this is how you handle object references in singletons.
    /// <br/>The object references are stored via SceneRefs.
    /// </summary>
    private void BindSceneRefs()
    {
        refs = FindFirstObjectByType<SceneRefs>(FindObjectsInactive.Include);
    }

    private void ApplyState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                if (refs?.mainMenuUI) refs.mainMenuUI.SetActive(true);
                break;
            case GameState.InGame:
                if (!hasBegun) StartCoroutine(OnGameStarted()); hasBegun = true;
                if (refs?.inGameScreen) refs.inGameScreen.SetActive(true);
                break;
            case GameState.Paused:
                if (!gamePaused) StartCoroutine(OnGamePaused()); gamePaused = true;
                break;
        }
    }

    public void Initiate()
    {
        ChangeState(GameState.InGame);
    }

    IEnumerator OnGameStarted()
    {
        // Fade out main menu
        if (refs?.mainMenuUI) objAnim?.AnimFadeOut(refs.mainMenuUI, 0.3f, true, true);
        yield return new WaitForSeconds(1f);
        if (refs?.inGameNameText) refs.inGameNameText.SetActive(true);
    }

    IEnumerator OnGamePaused()
    {
        // This will be animated later, but for now it'll just instantly pause.
        // if (refs?.inGameUI) refs.inGameUI.SetActive(false); use when we have a pause menu
        Time.timeScale = 0f;
        yield return null;
    }
}
