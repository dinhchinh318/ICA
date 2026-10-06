using UnityEngine;
namespace LumaReef.Core
{
    public enum GameState { Loading, Menu, Playing, Paused }
    public sealed class GameManager : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Loading;
        public void SetState(GameState state) { State=state; }
        void Awake() { Application.targetFrameRate=60; Application.runInBackground=true; Screen.orientation=ScreenOrientation.Portrait; }
    }
}
