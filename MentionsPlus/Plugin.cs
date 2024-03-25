#region Using

using Terraria;
using TerrariaApi.Server;

using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.Configuration;

using Mentions;

#endregion

namespace MentionsPlus
{
    [ApiVersion(2, 1)]
    public class MentionsPlusPlugin : TerrariaPlugin
    {
        #region Data

        public override string Author => "Zoom L1";
        public override string Name => "Mentions Plus";
        public override Version Version => new Version(1, 0, 2, 0);
        public MentionsPlusPlugin(Main game) : base(game) { }

        public static ConfigFile<ConfigSettings> Config = new ConfigFile<ConfigSettings>();
        static readonly int[] _shoots = new int[]
        {
            167, 168, 169, 170, 415, 416, 417, 418
        };
        static Random rand = new Random();
        static DateTime[] _cd = new DateTime[Main.maxNetPlayers];

        #endregion
        #region Initialize

        public override void Initialize()
        {
            OnReload(new ReloadEventArgs(null));
            GeneralHooks.ReloadEvent += OnReload;
            MentionsPlugin.Mentioned += OnMentioned;
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralHooks.ReloadEvent -= OnReload;
                MentionsPlugin.Mentioned -= OnMentioned;
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            }
            base.Dispose(disposing);
        }

        #endregion

        #region OnMentioned

        private void OnMentioned(MentionedHandledEventArgs args)
        {
            if (args.Handled)
                return;

            string name = args.Name.ToLower();

            args.Handled = true;
            if (args.Match.Groups["prefix"].Value == "!" && name == "everyone" && args.Author.HasPermission(EveryonePing))
            {
                args.Mentioned.Clear();
                args.Mentioned.AddRange(TShock.Players.Where(p => p != null && p.Active && p.State == 10));
                args.Result = $"[c/{Config.Settings.EveryoneHEX}:@everyone]";
            }
            else if (args.Match.Groups["prefix"].Value == "&" && args.Author.HasPermission(GroupPing))
            {
                args.Mentioned.AddRange(TShock.Players.Where(p => p != null && p.State == 10 && p.Group?.Name.ToLower() == name));
                args.Result = $"[c/{Config.Settings.GroupHEX}:@{args.Name}]";
            }
            else
                args.Handled = false;

            foreach (TSPlayer player in args.Mentioned)
            {
                if (_cd[player.Index] < DateTime.Now)
                {
                    if (!player.TPlayer.hostile && !player.TPlayer.dead)
                    {
                        if (player.TPlayer.statLife <= 1)
                            // Тут стояло пояснение от Syao где он просил добавить ожидание на 0.1 секунду,
                            // но я считаю что оно не особо то и хорошее. 
                            player.Heal(1);
                        player.DamagePlayer(1);
                    }

                    int projectile = Projectile.NewProjectile(Projectile.GetNoneSource(),
                        X: player.X, Y: player.Y - 64, SpeedX: 0, SpeedY: -8, Type: _shoots[rand.Next(_shoots.Length)],
                        Damage: 0, KnockBack: 0);
                    Main.projectile[projectile].Kill();

                    _cd[player.Index] = DateTime.Now.AddSeconds(Config.Settings.CoolDown);
                }
            }
        }

        #endregion

        #region OnJoin

        void OnJoin(JoinEventArgs args)
        {
            _cd[args.Who] = DateTime.MinValue;
        }

        #endregion

        #region OnReload

        public void OnReload(ReloadEventArgs args)
        {
            string path = Path.Combine(TShock.SavePath, Name + ".json");
            Config = new ConfigFile<ConfigSettings>();
            Config.Read(path, out bool write);
            if (write)
                Config.Write(path);

            args.Player?.SendSuccessMessage("[" + Name + "] Reloaded.");
        }

        #endregion
    }
}