using System.Threading.Tasks;
using UnityEngine;

namespace ClickerAdventure
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool autoDevLogin = false;

        private async void Start()
        {
            DesktopTestMode.Configure();
            await BootAsync();
        }

        private async Task BootAsync()
        {
            DontDestroyOnLoad(gameObject);
            await ServiceRegistry.InitializeAsync();

            if (ServiceRegistry.Auth.IsSignedIn)
            {
                await LoadChildDataAndOpenMap(ServiceRegistry.Auth.CurrentUser.ChildId);
                return;
            }

            if (autoDevLogin)
            {
                var user = await ServiceRegistry.Auth.DevLoginAsync();
                await LoadChildDataAndOpenMap(user.ChildId);
                return;
            }

            SceneLoader.LoadLogin();
        }

        private static async Task LoadChildDataAndOpenMap(string childId)
        {
            await ServiceRegistry.PlayerProfiles.LoadChildProfileAsync(childId);
            await ServiceRegistry.GameState.LoadAsync(childId);
            SceneLoader.LoadMap();
        }
    }
}
