using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClickerAdventure
{
    public class LoginScreen : MonoBehaviour
    {
        private TMP_InputField emailInput;
        private TMP_InputField passwordInput;
        private Text statusText;
        private bool isLoggingIn;

        private async void Start()
        {
            await ServiceRegistry.InitializeAsync();
            Build();
        }

        private void Update()
        {
            if (!DesktopTestMode.IsEnabled || isLoggingIn)
            {
                return;
            }

            if (DesktopTestMode.Pressed(KeyCode.Tab))
            {
                if (emailInput.isFocused)
                {
                    passwordInput.ActivateInputField();
                }
                else
                {
                    emailInput.ActivateInputField();
                }
            }

            if (DesktopTestMode.Pressed(KeyCode.Return) || DesktopTestMode.Pressed(KeyCode.KeypadEnter))
            {
                _ = LoginAsync(false);
            }

            if (DesktopTestMode.Pressed(KeyCode.F1))
            {
                _ = LoginAsync(true);
            }
        }

        private void Build()
        {
            var canvas = RuntimeUi.CreateCanvas();
            var loginBackground = ArtLibrary.Background("bg_login_adventure_gate");
            if (loginBackground != null)
            {
                RuntimeUi.SpriteBackground(canvas.transform, "LoginBackground", loginBackground, new Color(0.08f, 0.12f, 0.16f));
            }
            else
            {
                RuntimeUi.AdventureBackground(canvas.transform, new Color(0.18f, 0.37f, 0.54f), new Color(0.14f, 0.42f, 0.31f), new Color(0.08f, 0.12f, 0.16f));
            }

            var root = RuntimeUi.SafeAreaRoot(canvas.transform);
            var shade = RuntimeUi.Panel(root, "BackdropShade", new Color(0.03f, 0.09f, 0.10f, 0.42f));
            shade.GetComponent<Image>().raycastTarget = false;
            var hero = RuntimeUi.SpriteImage(root, "HeroPortrait", ArtLibrary.Hero("char_child_hero_portrait"));
            RuntimeUi.SetRect(hero.rectTransform, new Vector2(0.5f, 0.87f), new Vector2(200, 200), Vector2.zero);

            var title = RuntimeUi.Text(root, "Title", "Каждый день — новое приключение", 44, Color.white);
            title.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(title.rectTransform, new Vector2(0.5f, 0.75f), new Vector2(860, 130), Vector2.zero);

            var subtitle = RuntimeUi.Text(root, "Subtitle", "Реальные дела усиливают героя", 28, new Color(1f, 0.88f, 0.58f));
            RuntimeUi.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(820, 70), Vector2.zero);

            var loginCard = RuntimeUi.Card(root, "LoginCard", UiTheme.Paper);
            RuntimeUi.SetRect(loginCard, new Vector2(0.5f, 0.37f), new Vector2(840, 880), Vector2.zero);
            var heading = RuntimeUi.Text(loginCard, "Heading", "Добро пожаловать", 36, UiTheme.Ink);
            heading.fontStyle = FontStyle.Bold;
            RuntimeUi.SetRect(heading.rectTransform, new Vector2(0.5f, 0.89f), new Vector2(720, 64), Vector2.zero);
            var hint = RuntimeUi.Text(loginCard, "Hint", "Войди, чтобы продолжить путь героя", 24, UiTheme.SecondaryInk);
            RuntimeUi.SetRect(hint.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(720, 48), Vector2.zero);

            emailInput = CreateInput(loginCard, "Email", "Твоя электронная почта", new Vector2(0.5f, 0.67f));
            emailInput.contentType = TMP_InputField.ContentType.EmailAddress;
            passwordInput = CreateInput(loginCard, "Password", "Пароль", new Vector2(0.5f, 0.52f));
            passwordInput.contentType = TMP_InputField.ContentType.Password;

            var login = RuntimeUi.Button(loginCard, "LoginButton", "Начать приключение", UiTheme.Primary, () => _ = LoginAsync(false));
            RuntimeUi.SetRect(login.GetComponent<RectTransform>(), new Vector2(0.5f, 0.34f), new Vector2(650, 82), Vector2.zero);

            var dev = RuntimeUi.Button(loginCard, "DevLoginButton", "Демо-вход", UiTheme.Secondary, () => _ = LoginAsync(true));
            RuntimeUi.SetRect(dev.GetComponent<RectTransform>(), new Vector2(0.5f, 0.21f), new Vector2(650, 72), Vector2.zero);

            statusText = RuntimeUi.Text(loginCard, "Status", "", 26, RuntimeUi.Ink);
            RuntimeUi.SetRect(statusText.rectTransform, new Vector2(0.5f, 0.08f), new Vector2(720, 76), Vector2.zero);
        }

        private static TMP_InputField CreateInput(Transform parent, string name, string placeholder, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(Image), typeof(TMP_InputField), typeof(Shadow), typeof(Outline));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.95f, 0.91f, 0.82f);
            image.raycastTarget = true;
            go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.25f);
            go.GetComponent<Shadow>().effectDistance = new Vector2(0, -4);
            go.GetComponent<Outline>().effectColor = new Color(0.28f, 0.20f, 0.13f, 0.55f);
            UiTheme.ApplySurface(image, new Color(0.91f, 0.93f, 0.90f));
            var input = go.GetComponent<TMP_InputField>();
            input.targetGraphic = image;
            input.caretColor = RuntimeUi.Ink;
            input.selectionColor = new Color(0.21f, 0.51f, 0.76f, 0.35f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D))
                .GetComponent<RectTransform>();
            viewport.SetParent(go.transform, false);
            RuntimeUi.Stretch(viewport);
            input.textViewport = viewport;
            var text = CreateInputText(viewport, "Text", "", RuntimeUi.Ink);
            var placeholderText = CreateInputText(viewport, "Placeholder", placeholder, UiTheme.SecondaryInk);
            input.textComponent = text;
            input.placeholder = placeholderText;
            RuntimeUi.SetRect(go.GetComponent<RectTransform>(), anchor, new Vector2(720, 76), Vector2.zero);
            return input;
        }

        private static TextMeshProUGUI CreateInputText(Transform parent, string name, string value, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 30;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            RuntimeUi.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(24, 0);
            text.rectTransform.offsetMax = new Vector2(-24, 0);
            return text;
        }

        private async Task LoginAsync(bool dev)
        {
            if (isLoggingIn)
            {
                return;
            }

            isLoggingIn = true;
            statusText.text = "Загрузка профиля...";
            try
            {
                var user = dev
                    ? await ServiceRegistry.Auth.DevLoginAsync()
                    : await ServiceRegistry.Auth.SignInWithEmailAsync(emailInput.text, passwordInput.text);

                if (user.Role != UserRole.Child)
                {
                    statusText.text = "Этот клиент доступен только ребенку.";
                    return;
                }

                await ServiceRegistry.PlayerProfiles.LoadChildProfileAsync(user.ChildId);
                await ServiceRegistry.GameState.LoadAsync(user.ChildId);
                SceneLoader.LoadMap();
            }
            catch (System.Exception ex)
            {
                statusText.text = ex.Message;
            }
            finally
            {
                isLoggingIn = false;
            }
        }
    }
}
