using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Nida;
using Nida.Utility;
using HitChance = Nida.Utility.HitChance;
using Color = System.Drawing.Color;
using Prediction = Nida.Utility.Prediction;


namespace Nidalee
{
    internal class Nidalee
    {
        private static AIHeroClient Player { get { return ObjectManager.Player; } }


        public static Spell.Skillshot Q { get; private set; }

        public static Spell.Skillshot W { get; private set; }

        public static Spell.Targeted E { get; private set; }

        public static Spell.Active R { get; private set; }

        public static Menu ComboMenu { get; private set; }

        public static Menu DrawMenu { get; private set; }

        public static Menu HarassMenu { get; private set; }

        public static Menu LaneMenu { get; private set; }

        public static Menu jgMenu { get; private set; }

        public static Menu HealMenu { get; private set; }

        public static Menu FleeMenu { get; private set; }


        public static List<AIHeroClient> Enemies = new List<AIHeroClient>(), Allies = new List<AIHeroClient>();

        public static Menu AutoMenu { get; private set; }

        public static Menu PredMenu { get; private set; }

        private const string ChampionName = "Nidalee";

        private static Menu Menu, NidaMenu;

        private static bool CougarForm { get { return Q.Name == "Takedown"; } }

        private static int qhumancount, ehumancount, qcougarcount, wcougarcount, ecougarcount;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Chat.Print("Nidalee Loaded", Color.Red);
            if (ChampionName != Player.BaseSkinName)
            {
                return;
            }

            Q = new Spell.Skillshot(SpellSlot.Q, 1500, SkillShotType.Linear, 125, 1300, 40);
            Q.AllowedCollisionCount = 0;
            W = new Spell.Skillshot(SpellSlot.W, 875, SkillShotType.Circular, 500, 1450, 100);
            E = new Spell.Targeted(SpellSlot.E, 600);
            R = new Spell.Active(SpellSlot.R);


            NidaMenu = MainMenu.AddMenu("Nidalee", "Nidalee");
            NidaMenu.AddGroupLabel("Nidalee Sexy Hunter");

            PredMenu = NidaMenu.AddSubMenu("Prediction");
            PredMenu.AddGroupLabel("Prediction Settings");
            StringList(PredMenu, "Qpred", "Q Prediction", new[] { "Low", "Medium", "High", "Very High" }, 3);

            ComboMenu = NidaMenu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.Add("minGrab", new Slider("Min Q range", 250, 125, (int)Q.Range));
            ComboMenu.Add("maxGrab", new Slider("Max Q range", (int)Q.Range, 125, (int)Q.Range));
            ComboMenu.AddSeparator();
            ComboMenu.Add("QHumanz", new CheckBox("Use Q Human"));
            ComboMenu.Add("WHuman", new CheckBox("Use W Human"));
            ComboMenu.Add("useRalways", new CheckBox("Auto R switch on Marked Target"));
            ComboMenu.Add("useRalwayskillable", new CheckBox("Auto R switch on Killable"));
            ComboMenu.Add("useWalways", new CheckBox("Use W Cougar (Always)"));
            ComboMenu.Add("useWkillable", new CheckBox("Use W Cougar (Killable)"));
            ComboMenu.Add("useRbackhuman", new CheckBox("Auto switch forms"));


            HarassMenu = NidaMenu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("useQharass", new CheckBox("use Q harass"));
            ComboMenu.AddSeparator();
            HarassMenu.Add("manaharass", new Slider("if mana >", 30, 0, 100));

            jgMenu = NidaMenu.AddSubMenu("JungleClear");
            jgMenu.AddGroupLabel("JungleClear Settings");
            jgMenu.Add("useqjgh", new CheckBox("use Q human"));
            jgMenu.Add("usewjgh", new CheckBox("use W human"));
            jgMenu.Add("useqjgc", new CheckBox("use Q cougar"));
            jgMenu.Add("usewjgc", new CheckBox("use W cougar"));
            jgMenu.Add("useejgc", new CheckBox("use E cougar"));
            jgMenu.Add("autoswitchclear", new CheckBox("auto switch to cougar"));
            jgMenu.Add("autoswitchclear2", new CheckBox("auto switch to human"));



            var allies = EntityManager.Heroes.Allies.Where(a => !a.IsMe).OrderBy(a => a.BaseSkinName);

            HealMenu = NidaMenu.AddSubMenu("Heal");
            HealMenu.AddGroupLabel("Heal Settings");
            HealMenu.Add("heal", new CheckBox("Heal"));
            HealMenu.Add("rtoheal", new CheckBox("switch form to heal [not working]"));
            HealMenu.Add("lowhp", new Slider("if hp <", 30, 0, 100));
            foreach (var a in allies)
            {
                HealMenu.Add("autoHeal" + a.BaseSkinName, new CheckBox("Auto Heal " + a.BaseSkinName));
            }

            AutoMenu = NidaMenu.AddSubMenu("Misc");
            AutoMenu.AddGroupLabel("Misc Settings");
            AutoMenu.Add("ksQhuman", new CheckBox("Ks with Q human"));
            AutoMenu.Add("switchformks", new CheckBox("Switch form to ks"));

            FleeMenu = NidaMenu.AddSubMenu("Flee");
            FleeMenu.AddGroupLabel("Flee Settings");
            FleeMenu.Add("wflee", new CheckBox("Use W cougar"));
            FleeMenu.Add("fleeswitch", new CheckBox("Auto Switch to cougar"));


            DrawMenu = NidaMenu.AddSubMenu("Draw");
            DrawMenu.AddGroupLabel("Draw Settings");
            DrawMenu.Add("drawQ", new CheckBox("Draw Q"));




            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalker.OnPostAttack += OnPostAttack;
            Obj_AI_Base.OnProcessSpellCast += OnCast;
            Drawing.OnDraw += Drawing_OnDraw;



        }
        public static void StringList(Menu menu, string uniqueId, string displayName, string[] values, int defaultValue)
        {
            var mode = menu.Add(uniqueId, new Slider(displayName, defaultValue, 0, values.Length - 1));
            mode.DisplayName = displayName + ": " + values[mode.CurrentValue];
            mode.OnValueChange +=
                delegate (ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
                {
                    sender.DisplayName = displayName + ": " + values[args.NewValue];
                };
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (DrawMenu["drawQ"].Cast<CheckBox>().CurrentValue)
                Drawing.DrawCircle(Player.Position, Q.Range, Color.Red);


        }


        private static void OnCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            var spell = args.SData;
            if (spell.Name == "Swipe")
            {
                ecougarcount = Core.GameTickCount;

                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Q.IsReady())
                {
                    Core.DelayAction(() => Q.Cast(Player.Position), 300 - Game.Ping / 2);
                }
            }
            if (spell.Name == "Pounce")
            {
                wcougarcount = Core.GameTickCount;

            }
            if (spell.Name == "Takedown")
            {
                qcougarcount = Core.GameTickCount;
                Orbwalker.ResetAutoAttack();
            }
            if (spell.Name == "JavelinToss")
            {
                qhumancount = Core.GameTickCount;
            }
            if (spell.Name == "PrimalSurge")
            {
                ehumancount = Core.GameTickCount;
            }
        }


        static void OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!Player.IsMe) return;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (CougarForm && E.IsReady())
                {

                    E.Cast(target.Position);
                }

                if (!CougarForm && Q.IsReady())
                {
                    Core.DelayAction(() => castQtarget(target), 500);
                }

            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && HarassMenu["useQharass"].Cast<CheckBox>().CurrentValue && Player.Mana * 100 / Player.MaxHealth >= HarassMenu["manaharass"].Cast<Slider>().CurrentValue)
            {
                if (!CougarForm && Q.IsReady())
                {
                    Core.DelayAction(() => castQtarget(target), 500);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Player.IsRecalling())
                Misc();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                Flee();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                JungleClear();

        }

        private static void Misc()
        {

            if (!CougarForm && Q.IsReady() && AutoMenu["ksQhuman"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var x in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && !x.IsZombie))
                {
                    if (Qhumandamage(x) > x.Health)
                    {
                        CastSpell(Q, x, predQ(), ComboMenu["maxGrab"].Cast<Slider>().CurrentValue);
                    }
                }
                if (CougarForm && QhumanReady && R.IsReady() && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) && AutoMenu["switchformks"].Cast<CheckBox>().CurrentValue)
                {
                    foreach (var x in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && !x.IsZombie))
                    {
                        if (Qhumandamage(x) > x.Health)
                        {
                            R.Cast(x);
                        }
                    }
                }
                if (!CougarForm && E.IsReady() && HealMenu["heal"].Cast<CheckBox>().CurrentValue)
                {
                    foreach (var x in EntityManager.Heroes.Allies.Where(x => x.IsValidTarget(E.Range, false) && !x.IsZombie))
                    {
                        if (x.Health * 100 / x.MaxHealth <= HealMenu["lowhp"].Cast<Slider>().CurrentValue)
                        {
                            E.Cast(x);
                        }
                    }
                }
                if (CougarForm && EhumanReady && R.IsReady() && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.None) && HealMenu["rtoheal"].Cast<CheckBox>().CurrentValue))
                {
                    foreach (var x in EntityManager.Heroes.Allies.Where(x => x.IsValidTarget(E.Range, false) && !x.IsZombie))
                    {
                        if (x.Health * 100 / x.MaxHealth <= HealMenu["lowhp"].Cast<Slider>().CurrentValue)
                        {
                            R.Cast(x);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            var jgminion =

                EntityManager.MinionsAndMonsters.GetJungleMonsters()
                    .OrderByDescending(j => j.Health)
                    .FirstOrDefault(j => j.IsValidTarget(Q.Range));
            {
                if (jgminion == null) return;

                if (!CougarForm && Q.IsReady() && jgminion.IsValidTarget(Q.Range) && jgMenu["useqjgh"].Cast<CheckBox>().CurrentValue)
                {
                    Q.Cast(jgminion);
                }

                if (!CougarForm && W.IsReady() && jgMenu["usewjgh"].Cast<CheckBox>().CurrentValue)
                {
                    W.Cast(jgminion.Position);
                }
                if (R.IsReady() && !Q.IsReady() && jgMenu["autoswitchclear"].Cast<CheckBox>().CurrentValue)
                {
                    R.Cast();
                }
                if ((CougarForm && Q.IsReady() && jgminion.IsValidTarget(Q.Range) && jgMenu["useqjgc"].Cast<CheckBox>().CurrentValue))
                {
                    Q.Cast(jgminion);
                }

                if ((CougarForm && W.IsReady() && jgminion.HasBuff("nidaleepassivehunted") && jgminion.IsValidTarget(W.Range) && jgMenu["usewjgc"].Cast<CheckBox>().CurrentValue))
                {
                    W.Cast(jgminion);
                }


                if ((CougarForm && E.IsReady() && jgminion.IsValidTarget(E.Range) && jgMenu["useejgc"].Cast<CheckBox>().CurrentValue))
                {
                    E.Cast(jgminion);
                }
            }
        }



        private static void Flee()
        {
            if (FleeMenu["fleeswitch"].Cast<CheckBox>().CurrentValue && !CougarForm)
            {
                if (R.IsReady())
                {
                    R.Cast();
                }
            }
            if (FleeMenu["wflee"].Cast<CheckBox>().CurrentValue && CougarForm)
            {
                var tempPos = Game.CursorPos;
                if (tempPos.IsInRange(Player.Position, W.Range))
                {
                    W.Cast(tempPos);
                }
                else if (FleeMenu["wflee"].Cast<CheckBox>().CurrentValue && CougarForm)
                {
                    W.Cast(Player.Position.Extend(tempPos, 800).To3DWorld());
                }
            }
        }


        private static void Harass()
        {
            if (!CougarForm && HarassMenu["useQharass"].Cast<CheckBox>().CurrentValue && Player.Mana * 100 / Player.MaxHealth >= HarassMenu["manaharass"].Cast<Slider>().CurrentValue && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                var target2 = Orbwalker.LastTarget;
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    if (!EloBuddy.Player.Instance.IsInAutoAttackRange(target))
                    {
                        CastSpell(Q, target, predQ(), ComboMenu["maxGrab"].Cast<Slider>().CurrentValue);
                    }
                    else if (target.NetworkId != target2.NetworkId)
                    {
                        CastSpell(Q, target, predQ(), ComboMenu["maxGrab"].Cast<Slider>().CurrentValue);
                    }
                }
            }
        }

        private static void Combo()
        {
            if (!CougarForm && ComboMenu["QHumanz"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                var target2 = Orbwalker.LastTarget;
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    if (!EloBuddy.Player.Instance.IsInAutoAttackRange(target))
                    {
                        CastSpell(Q, target, predQ(), ComboMenu["maxGrab"].Cast<Slider>().CurrentValue);
                    }
                    else if (target.NetworkId != target2.NetworkId)
                    {
                        CastSpell(Q, target, predQ(), ComboMenu["maxGrab"].Cast<Slider>().CurrentValue);
                    }
                }
            }

            if (!CougarForm && W.IsReady() && ComboMenu["WHuman"].Cast<CheckBox>().CurrentValue)
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (target.IsValidTarget() && !target.IsZombie)
                    W.Cast(target.Position);

            }



            if (!CougarForm && R.IsReady() && ComboMenu["useRalways"].Cast<CheckBox>().CurrentValue)
            {
                var heroes = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 750)
                    .OrderByDescending(x => x.Health).LastOrDefault();
                if (heroes.IsValidTarget() && WcougarReady)
                {
                    R.Cast();
                }
            }

            if (!CougarForm && R.IsReady() && ComboMenu["useRalwayskillable"].Cast<CheckBox>().CurrentValue)
            {
                var heroes = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 750)
                    .OrderByDescending(x => x.Health);
                foreach (var x in heroes)
                {
                    if (x.Health - Wcougardamage(x) * 2 - Ecougardamage(x) - Qcougardamage(x) < 0)
                    {
                        R.Cast();
                        return;
                    }
                }
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && !x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 375 + Player.BoundingRadius);
                foreach (var x in targets)
                {
                    if (x.Health - Wcougardamage(x) - Ecougardamage(x) - Qcougardamage(x) < 0)
                    {
                        R.Cast();
                        return;
                    }
                }
            }


            if (CougarForm && W.IsReady() && ComboMenu["useWalways"].Cast<CheckBox>().CurrentValue)
            {
                var heroes = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 750)
                    .OrderByDescending(x => 1 - (x.Health - Wcougardamage(x) - Ecougardamage(x) - Qcougardamage(x)));
                foreach (var x in heroes)
                {
                    W.Cast(x.Position);
                    Q.Cast(x);
                    return;

                }
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && !x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 375 + Player.BoundingRadius)
                   .OrderByDescending(x => 1 - (x.Health - Wcougardamage(x) * 2 - Ecougardamage(x) - Qcougardamage(x)));
                foreach (var x in heroes)
                {
                    W.Cast(x.Position);
                    return;
                }
            }

            if (CougarForm && W.IsReady() && ComboMenu["useWkillable"].Cast<CheckBox>().CurrentValue)
            {
                var heroes = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 750
                     && x.Health - Wcougardamage(x) * 2 - Ecougardamage(x) - Qcougardamage(x) < 0)
                    .OrderByDescending(x => 1 - x.Health);
                foreach (var x in heroes)
                {
                    W.Cast(x.Position);
                    return;
                }
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget() && !x.IsZombie && !x.HasBuff("nidaleepassivehunted") && x.Distance(Player.Position) <= 375 + Player.BoundingRadius
                    && x.Health - Wcougardamage(x) * 2 - Ecougardamage(x) - Qcougardamage(x) < 0)
                    .OrderByDescending(x => 1 - x.Health);
                foreach (var x in targets)
                {
                    W.Cast(x.Position);
                    return;
                }

            }
            if (CougarForm && R.IsReady() && ComboMenu["useRbackhuman"].Cast<CheckBox>().CurrentValue)
            {
                if (QhumanReady)
                    R.Cast();
            }
        }
        private static bool QhumanReady
        {
            get
            {
                return
                    Player.Mana >= new int[] { 50, 60, 70, 80, 90 }[Q.Level - 1]
                    && Core.GameTickCount - qhumancount >= 6 * (1 - Player.PercentCooldownMod);
            }
        }
        private static bool EhumanReady
        {
            get
            {
                return
                    Player.Mana >= new int[] { 60, 75, 90, 105, 120 }[E.Level - 1]
                    && Core.GameTickCount - qhumancount >= 12 * (1 - Player.PercentCooldownMod);
            }
        }
        private static bool QcougarReady
        {
            get
            {
                return Core.GameTickCount - qcougarcount >= 5 * (1 - Player.PercentCooldownMod);
            }
        }
        private static bool EcougarReady
        {
            get
            {
                return Core.GameTickCount - ecougarcount >= 5 * (1 - Player.PercentCooldownMod);
            }
        }
        private static bool WcougarReady
        {
            get
            {
                return (CougarForm && W.IsReady()) ? true : Core.GameTickCount - wcougarcount >= 5 * (1 - Player.PercentCooldownMod);
            }
        }
        private static void castQtarget(AttackableUnit target)
        {
            if (target.IsValidTarget() && !target.IsZombie)
            {
                var castpos = Q.GetPrediction(target as Obj_AI_Base).CastPosition;
                var collisions = Q.GetPrediction(target as Obj_AI_Base).CollisionObjects;
                if (!collisions.Any() && Q.IsReady() && !CougarForm)
                    Q.Cast(castpos);
            }
        }

        private static void CastSpell(Spell.Skillshot QWER, Obj_AI_Base target, HitChance hitchance, int MaxRange)
        {
            var coreType2 = SkillshotType.SkillshotLine;
            var aoe2 = false;
            if ((int)QWER.Type == (int)SkillshotType.SkillshotCircle)
            {
                coreType2 = SkillshotType.SkillshotCircle;
                aoe2 = true;
            }
            if (QWER.Width > 80 && QWER.AllowedCollisionCount < 100)
                aoe2 = true;
            var predInput2 = new PredictionInput
            {
                Aoe = aoe2,
                Collision = QWER.AllowedCollisionCount < 100,
                Speed = QWER.Speed,
                Delay = QWER.CastDelay,
                Range = MaxRange,
                From = Player.ServerPosition,
                Radius = QWER.Radius,
                Unit = target,
                Type = coreType2
            };
            var poutput2 = Prediction.GetPrediction(predInput2);
            if (QWER.Speed < float.MaxValue && Utils.CollisionYasuo(Player.ServerPosition, poutput2.CastPosition))
                return;

            if (hitchance == HitChance.VeryHigh)
            {
                if (poutput2.Hitchance >= HitChance.VeryHigh)
                    QWER.Cast(poutput2.CastPosition);
                else if (predInput2.Aoe && poutput2.AoeTargetsHitCount > 1 &&
                         poutput2.Hitchance >= HitChance.High)
                {
                    QWER.Cast(poutput2.CastPosition);
                }
            }
            else if (hitchance == HitChance.High)
            {
                if (poutput2.Hitchance >= HitChance.High)
                    QWER.Cast(poutput2.CastPosition);
            }
            else if (hitchance == HitChance.Medium)
            {
                if (poutput2.Hitchance >= HitChance.Medium)
                    QWER.Cast(poutput2.CastPosition);
            }
            else if (hitchance == HitChance.Low)
            {
                if (poutput2.Hitchance >= HitChance.Low)
                    QWER.Cast(poutput2.CastPosition);
            }
        }

        public static bool HasSpell(string s)
        {
            return EloBuddy.Player.Spells.FirstOrDefault(o => o.SData.Name.Contains(s)) != null;
        }

        private static HitChance predQ()
        {
            switch (PredMenu["Qpred"].Cast<Slider>().CurrentValue)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
            }
            return HitChance.Medium;
        }

        public static double Qhumandamage(Obj_AI_Base target)
        {
            return (new double[] { 80, 125, 170, 215, 260 }[W.Level - 1] +
                    ((Player.MaxHealth - (498.48f + (86f * (Player.Level - 1f)))) * 0.15f)) *
                   ((target.MaxHealth - target.Health) / target.MaxHealth + 1);
        }

        private static float Qcougardamage(Obj_AI_Base target)
        {
            if (!CougarForm)
            {
                var dist = target.Distance(Player);
                var extraDmg2 = 0;

                for (double i = 0; i < (dist); i++)
                {
                    i = i + 3.875;
                    extraDmg2 += 1;
                }

                var finalExtra2 = extraDmg2 <= 200f ? extraDmg2 / 100f : 2f;

                return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                    (new[] { 0f, 60f, 77.5f, 95f, 112.5f, 130f }[Q.Level] +
                     (Player.TotalMagicalDamage * (0.4f)))) * finalExtra2;
            }

            if (CougarForm)
            {
                var missingHealth = (target.MaxHealth - target.Health) / 100f;
                var extraDmg = 0f;
                for (var i = 0; i < missingHealth; i++)
                {
                    extraDmg += 1.5f;
                }
                var finalExtra = extraDmg <= 150f ? (extraDmg / 100f) : 1.5f;

                if (target.HasBuff("nidaleepassivehunted")) ;
                {
                    return Player.CalculateDamageOnUnit(target, DamageType.Mixed,
                        (new[] { 0f, 5.3f, 26.7f, 66.7f, 120f }[R.Level] +
                         (Player.TotalAttackDamage * finalExtra) +
                         (Player.TotalMagicalDamage * (0.48f * finalExtra))));
                }
                return Player.CalculateDamageOnUnit(target, DamageType.Mixed,
                    (new[] { 0f, 4f, 20f, 50f, 90f }[R.Level] +
                     (Player.TotalAttackDamage * (0.75f * finalExtra)) +
                     (Player.TotalMagicalDamage * (0.36f * finalExtra))));
            }

            return 0f;
        }

        private static float Wcougardamage(Obj_AI_Base target)
        {
            if (!CougarForm)
            {
                return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                    new[] { 0, 10, 20, 30, 40, 50 }[W.Level] + (Player.TotalMagicalDamage * 0.05f));
            }
            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] { 0, 50, 100, 150, 200 }[R.Level] + (Player.TotalMagicalDamage * 0.3f));
        }


        private static float Ecougardamage(Obj_AI_Base target)
        {
            if (!CougarForm)
            {
                return 0f;
            }
            return Player.CalculateDamageOnUnit(target, DamageType.Magical,
                new[] { 0, 70, 130, 190, 250 }[R.Level] + (Player.TotalMagicalDamage * 0.45f));
        }

        public static void checkbuff()
        {
            String temp = "";
            foreach (var buff in Player.Buffs)
            {
                temp += (buff.Name + "(" + buff.Count + ")" + "(" + buff.Type.ToString() + ")" + ", ");
            }
            Chat.Say(temp);
        }
    }
}