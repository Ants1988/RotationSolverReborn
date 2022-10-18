using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.Gui;
using System;
using System.Collections.Generic;
using XIVAutoAttack.Actions;
using XIVAutoAttack.Combos.CustomCombo;
using XIVAutoAttack.Configuration;

namespace XIVAutoAttack.Combos.RangedPhysicial;

internal class MCHCombo : JobGaugeCombo<MCHGauge>
{
    internal override uint JobID => 31;
    private static bool initFinished = false;
    private static bool MCH_Asocial = false;
    private static bool MCH_Opener = false;
    private static bool MCH_Automaton = false;
    internal struct Actions
    {
        public static readonly BaseAction
            //分裂弹
            SplitShot = new(2866),

            //独头弹
            SlugShot = new(2868)
            {
                OtherIDsCombo = new[] { 7411u },
            },

            //狙击弹
            CleanShot = new(2873)
            {
                OtherIDsCombo = new[] { 7412u },
            },

            //热冲击
            HeatBlast = new(7410),

            //散射
            SpreadShot = new(2870),

            //自动弩
            AutoCrossbow = new(16497),

            //热弹
            HotShow = new(2872),

            //空气锚
            AirAnchor = new(16500)
            {
                //过热不释放技能
                OtherCheck = b => !JobGauge.IsOverheated,
            },

            //钻头
            Drill = new(16498)
            {
                //过热不释放技能
                OtherCheck = b => !JobGauge.IsOverheated,
            },

            //回转飞锯
            ChainSaw = new(25788)
            {
                //过热不释放技能,打进爆发
                OtherCheck = b =>
                {
                    if (MCH_Asocial || !MCH_Opener) return true;
                    if (initFinished && !JobGauge.IsOverheated) return true;
                    return false;
                },
            },

            //毒菌冲击
            Bioblaster = new(16499, isDot: true)
            {
                //过热不释放技能
                OtherCheck = b => !JobGauge.IsOverheated,
            },

            //整备
            Reassemble = new(2876)
            {
                BuffsProvide = new ushort[] { ObjectStatus.Reassemble },
                OtherCheck = b => HaveHostileInRange,
            },

            //超荷
            Hypercharge = new(17209)
            {
                OtherCheck = b =>
                {
                    var isBoss = Target.IsBoss();

                    //在过热状态或者热量小于50时不释放超荷
                    if (JobGauge.IsOverheated || JobGauge.Heat < 50) return false;
                    if (!isBoss && IsTargetDying) return false;

                    //有野火buff必须释放超荷
                    if (StatusHelper.HaveStatusSelfFromSelf(ObjectStatus.Wildfire)) return true;

                    //在三大金刚还剩8秒冷却好时不释放超荷
                    if (Level >= Drill.Level && Drill.RecastTimeRemain < 8) return false;
                    if (Level >= AirAnchor.Level && AirAnchor.RecastTimeRemain < 8) return false;
                    if (Level >= ChainSaw.Level && ChainSaw.RecastTimeRemain < 8) return false;

                    //小怪AOE或者自嗨期间超荷判断
                    if ((SpreadShot.ShouldUse(out _) || !isBoss) && IsMoving) return false;
                    if (((SpreadShot.ShouldUse(out _) || !isBoss) && !IsMoving) || MCH_Asocial || Level < Wildfire.Level) return true;

                    uint wfTimer = 6;
                    var wildfireCDTime = Wildfire.RecastTimeRemain;
                    if (Level < BarrelStabilizer.Level) wfTimer = 12;

                    //标准循环起手判断
                    if (!initFinished && MCH_Opener) return false;

                    //野火前攒热量
                    if (15 < wildfireCDTime && wildfireCDTime < 43)
                    {
                        //如果期间热量溢出超过5,就释放一次超荷
                        if (LastWeaponskill == Drill.ID && JobGauge.Heat >= 85) return true;
                        return false;
                    }

                    //超荷释放判断
                    if (wildfireCDTime >= wfTimer
                    || LastWeaponskill == ChainSaw.ID
                    || (LastWeaponskill != ChainSaw.ID && (!Wildfire.IsCoolDown || wildfireCDTime <= 1))) return true;

                    return false;
                },
            },

            //野火
            Wildfire = new(2878)
            {
                OtherCheck = b =>
                {
                    var isBoss = Target.MaxHp / LocalPlayer.MaxHp > 9.5;
                    //小怪AOE期间不打野火
                    if (SpreadShot.ShouldUse(out _) || !isBoss) return false;
                    if (!isBoss && b.IsDying()) return false;

                    //热量低于50且上一个能力技不是超荷时不释放
                    if (JobGauge.Heat < 50 && LastAbility != Hypercharge.ID) return false;

                    //自嗨判断
                    if (MCH_Asocial)
                    {
                        if (Level >= Drill.Level && Drill.RecastTimeRemain < 10) return false;
                        if (Level >= AirAnchor.Level && AirAnchor.RecastTimeRemain < 10) return false;
                        if (Level >= ChainSaw.Level && ChainSaw.RecastTimeRemain < 10) return false;

                        return true;
                    }

                    if (!initFinished && MCH_Opener) return false;

                    if (JobGauge.IsOverheated) return true;

                    if (LastWeaponskill != ChainSaw.ID
                    && (LastWeaponskill == Drill.ID || LastWeaponskill == AirAnchor.ID || LastWeaponskill == HeatBlast.ID)) return false;

                    return true;
                },
            },

            //虹吸弹
            GaussRound = new(2874),

            //弹射
            Ricochet = new(2890),

            //枪管加热
            BarrelStabilizer = new(7414)
            {
                OtherCheck = b =>
                {
                    if (JobGauge.Heat <= 50 && LastWeaponskill != ChainSaw.ID) return true;

                    return false;
                },
            },

            //车式浮空炮塔
            RookAutoturret = new(2864)
            {
                OtherCheck = b =>
                {
                    var isBoss = Target.MaxHp / LocalPlayer.MaxHp > 9.5;

                    //电量等于100,强制释放
                    if (JobGauge.Battery == 100) return true;
                    if (!isBoss && b.IsDying()) return false;

                    //基本判断
                    if (JobGauge.Battery < 50 || JobGauge.IsRobotActive) return false;

                    //自嗨与小怪AOE判断
                    if (MCH_Asocial || !MCH_Automaton || (!isBoss && !IsMoving) || Level < Wildfire.ID) return true;
                    if ((SpreadShot.ShouldUse(out _) || !isBoss) && IsMoving) return false;

                    //起手判断
                    if (!initFinished && MCH_Opener) return false;

                    //机器人吃团辅判断
                    if (AirAnchor.RecastTimeRemain < 5 && JobGauge.Battery > 80) return true;
                    if (ChainSaw.RecastTimeRemain < 5 || (ChainSaw.RecastTimeRemain > 55 && JobGauge.Battery <= 60)) return true;

                    return false;
                },
            },

            //策动
            Tactician = new(16889, true)
            {
                BuffsProvide = new[]
                {
                    ObjectStatus.Troubadour,
                    ObjectStatus.Tactician1,
                    ObjectStatus.Tactician2,
                    ObjectStatus.ShieldSamba,
                },
            };
    }
    internal override SortedList<DescType, string> Description => new()
    {
        {DescType.循环说明, $"请优先使用标准循环,即关闭自嗨循环.\n                     标准循环会在野火前攒热量来打偶数分钟爆发.\n                     AOE和攻击小怪时不会释放野火"},
        {DescType.爆发技能, $"{Actions.Wildfire.Action.Name}"},
        {DescType.范围防御, $"{Actions.Tactician.Action.Name}"},
    };

    private protected override ActionConfiguration CreateConfiguration()
    {
        return base.CreateConfiguration()
            .SetBool("MCH_Opener", true, "标准起手")
            .SetBool("MCH_Automaton", true, "机器人吃团辅")
            .SetBool("MCH_Reassemble", true, "整备优先链锯")
            .SetBool("MCH_Asocial", true, "自嗨循环(没有起手)");
    }

    private protected override bool DefenceAreaAbility(byte abilityRemain, out IAction act)
    {
        //策动
        if (Actions.Tactician.ShouldUse(out act, mustUse: true)) return true;

        return false;
    }

    private protected override bool BreakAbility(byte abilityRemain, out IAction act)
    {
        //野火
        if (Actions.Wildfire.ShouldUse(out act)) return true;

        act = null;
        return false;
    }

    private protected override bool GeneralGCD(uint lastComboActionID, out IAction act)
    {
        MCH_Opener = Config.GetBoolByName("MCH_Opener");
        MCH_Automaton = Config.GetBoolByName("MCH_Automaton");
        MCH_Asocial = Config.GetBoolByName("MCH_Asocial");

        //当上一个连击是热阻击弹时完成起手
        if (TargetHelper.InBattle && (lastComboActionID == Actions.CleanShot.ID || Actions.Wildfire.RecastTimeRemain > 10 || Actions.SpreadShot.ShouldUse(out _)))
        {
            initFinished = true;
        }

        //不在战斗中时重置起手
        if (!TargetHelper.InBattle)
        {
            //开场前整备,空气锚和钻头必须冷却好
            if ((!Actions.AirAnchor.IsCoolDown || !Actions.Drill.IsCoolDown) && Actions.Reassemble.ShouldUse(out act, emptyOrSkipCombo: true)) return true;
            initFinished = false;
        }

        if (!Config.GetBoolByName("MCH_Opener")) initFinished = true;

        //AOE,毒菌冲击
        if (Actions.Bioblaster.ShouldUse(out act)) return true;
        //单体,四个牛逼的技能。先空气锚再钻头
        if (Actions.AirAnchor.ShouldUse(out act)) return true;
        else if (Level < Actions.AirAnchor.Level && Actions.HotShow.ShouldUse(out act)) return true;
        if (Actions.Drill.ShouldUse(out act)) return true;
        if (Actions.ChainSaw.ShouldUse(out act, mustUse: true)) return true;

        //群体常规GCD
        if (JobGauge.IsOverheated && Actions.AutoCrossbow.ShouldUse(out act)) return true;
        if (Actions.SpreadShot.ShouldUse(out act)) return true;

        //单体常规GCD
        if (JobGauge.IsOverheated && Actions.HeatBlast.ShouldUse(out act)) return true;
        if (Actions.CleanShot.ShouldUse(out act, lastComboActionID)) return true;
        if (Actions.SlugShot.ShouldUse(out act, lastComboActionID)) return true;
        if (Actions.SplitShot.ShouldUse(out act, lastComboActionID)) return true;

        return false;
    }

    private protected override bool EmergercyAbility(byte abilityRemain, IAction nextGCD, out IAction act)
    {
        //等级小于钻头时,绑定狙击弹
        if (Level < Actions.Drill.Level && nextGCD == Actions.CleanShot)
        {
            if (Actions.Reassemble.ShouldUse(out act, emptyOrSkipCombo: true)) return true;
        }
        //等级小于90时或自嗨时,整备不再留层数
        if ((Level < Actions.ChainSaw.Level || !Config.GetBoolByName("MCH_Reassemble")) 
            && (nextGCD == Actions.AirAnchor || nextGCD == Actions.Drill || nextGCD == Actions.ChainSaw))
        {
            if (Actions.Reassemble.ShouldUse(out act, emptyOrSkipCombo: true)) return true;
        }
        //整备优先链锯
        if (Config.GetBoolByName("MCH_Reassemble") && nextGCD == Actions.ChainSaw)
        {
            if (Actions.Reassemble.ShouldUse(out act, emptyOrSkipCombo: true)) return true;
        }
        //如果接下来要搞三大金刚了，整备吧！
        if (nextGCD == Actions.AirAnchor || nextGCD == Actions.Drill)
        {
            if (Actions.Reassemble.ShouldUse(out act)) return true;
        }
        return base.EmergercyAbility(abilityRemain, nextGCD, out act);
    }

    private protected override bool ForAttachAbility(byte abilityRemain, out IAction act)
    {
        //起手虹吸弹、弹射
        if (Actions.Ricochet.RecastTimeRemain == 0 && Actions.Ricochet.ShouldUse(out act, mustUse: true)) return true;
        if (Actions.GaussRound.RecastTimeRemain == 0 && Actions.GaussRound.ShouldUse(out act, mustUse: true)) return true;

        //枪管加热
        if (Actions.BarrelStabilizer.ShouldUse(out act)) return true;

        //车式浮空炮塔
        if (Actions.RookAutoturret.ShouldUse(out act, mustUse: true)) return true;

        //超荷
        if (Actions.Hypercharge.ShouldUse(out act)) return true;

        if (Actions.GaussRound.RecastTimeRemain > Actions.Ricochet.RecastTimeRemain)
        {
            //弹射
            if (Actions.Ricochet.ShouldUse(out act, mustUse: true)) return true;
        }
        //虹吸弹
        if (Actions.GaussRound.ShouldUse(out act, mustUse: true)) return true;

        act = null!;
        return false;
    }
}
