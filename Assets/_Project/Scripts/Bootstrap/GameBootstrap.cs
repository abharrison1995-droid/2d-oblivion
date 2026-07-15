using UnityEngine;
using UnityEngine.SceneManagement;

namespace Voidovia
{
    /// <summary>
    /// Optional entry: load WorldMap where AppFlow runs character creation.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] string worldMapScene = "WorldMap";

        void Start()
        {
            if (SceneManager.GetActiveScene().name != worldMapScene)
                SceneManager.LoadScene(worldMapScene);
        }
    }
}
