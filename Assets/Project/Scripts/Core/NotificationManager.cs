using UnityEngine;
using AIStartupTycoon.Utils;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Schedules a local "come back and collect" notification whenever the player leaves the
    /// app, mirroring GameManager's own offline-earnings math so the estimate it advertises is
    /// honest. Android-only (guarded out everywhere else, including the Editor - Unity's
    /// notification APIs don't run there) - this can't be verified through in-Editor testing,
    /// only a real device/emulator build.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        private const string ChannelId = "aistartuptycoon_reminders";

        [Header("Reminder Timing")]
        [Tooltip("How long after the player leaves before the reminder fires.")]
        public float reminderDelayHours = 4f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

#if UNITY_ANDROID && !UNITY_EDITOR
            var channel = new AndroidNotificationChannel
            {
                Id = ChannelId,
                Name = "Company Updates",
                Importance = Importance.Default,
                Description = "Reminders about your company's progress while you're away."
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
        }

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Required on Android 13+ (our target API 36) - without it, notifications are
            // scheduled successfully but silently never shown.
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ScheduleReminder();
            else CancelReminder(); // player's back - no need to remind them to come back
        }

        private void OnApplicationQuit()
        {
            ScheduleReminder();
        }

        private void ScheduleReminder()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            CancelReminder(); // clear any previous one first, so pause immediately followed by quit doesn't double-book

            string body = "Your team is still shipping features. Come check on your startup!";
            if (GameManager.Instance != null && CurrencyManager.Instance != null)
            {
                BigNumber passivePerSecond = GameManager.Instance.GetTotalPassiveOutput();
                double rateMultiplier = CurrencyManager.Instance.PassiveOutputMultiplier
                    * CurrencyManager.Instance.GlobalEarningsMultiplier
                    * CurrencyManager.Instance.ReputationMultiplier;
                double estimatedEarnings = passivePerSecond.ToDouble() * rateMultiplier * reminderDelayHours * 3600.0;

                if (estimatedEarnings > 1.0)
                    body = $"Your team shipped ~${(BigNumber)estimatedEarnings} while you were away. Come collect it!";
            }

            var notification = new AndroidNotification
            {
                Title = "AI Startup Tycoon",
                Text = body,
                FireTime = System.DateTime.Now.AddHours(reminderDelayHours)
            };
            AndroidNotificationCenter.SendNotification(notification, ChannelId);
#endif
        }

        private void CancelReminder()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif
        }
    }
}
