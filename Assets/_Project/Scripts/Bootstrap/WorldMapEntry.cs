using UnityEngine;

namespace Voidovia
{
    /// <summary>
    /// Lets you press Play on WorldMap without opening Bootstrap first.
    /// </summary>
    public class WorldMapEntry : MonoBehaviour
    {
        void Awake()
        {
            // AppFlow owns boot + character creation when present.
            if (GetComponent<AppFlow>() != null || FindObjectOfType<AppFlow>() != null)
                return;

            if (GameState.Instance != null)
                return;

            gameObject.AddComponent<AppFlow>();
        }
    }
}
