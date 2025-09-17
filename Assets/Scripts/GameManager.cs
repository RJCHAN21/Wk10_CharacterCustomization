using UnityEngine;
using UnityEngine.SceneManagement;

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

    private SceneRefs refs;
    private ObjectAnimator objAnim;

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
                if (refs?.mainMenuUI) objAnim?.AnimFadeOut(refs.mainMenuUI, 0.3f, true, true);
                break;
            case GameState.Paused:
                break;
        }
    }

    public void Initiate()
    {
        ChangeState(GameState.InGame);
    }

    public void BeginCharacterCreation()
    {
        if (refs?.inGameNameText) refs.inGameNameText.SetActive(true);
    }
}
