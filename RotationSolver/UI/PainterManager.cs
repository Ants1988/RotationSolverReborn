﻿using ECommons.DalamudServices;
using ECommons.GameHelpers;
using XIVPainter.Element3D;

namespace RotationSolver.UI;

internal static class PainterManager
{
    class PositionalDrawing : Drawing3DPoly
    {
        Drawing3DCircularSectorFO _noneCir, _flankCir, _rearCir;

        public EnemyPositional Positional { get; set; }

        public GameObject Target { get; set; }

        public PositionalDrawing()
        {
            var right = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.15f));
            var wrong = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.8f, 0.2f, 0.15f));

            _noneCir = new Drawing3DCircularSectorFO(null, 3, wrong, 2);

            _flankCir = new Drawing3DCircularSectorFO(null, 3, wrong, 2, XIVPainter.Enum.RadiusInclude.IncludeBoth,
                new Vector2(MathF.PI * 0.25f, MathF.PI / 2), new Vector2(MathF.PI * 1.25f, MathF.PI / 2));
            _rearCir = new Drawing3DCircularSectorFO(null, 3, wrong, 2, XIVPainter.Enum.RadiusInclude.IncludeBoth,
                new Vector2(MathF.PI * 0.75f, MathF.PI / 2));

            _noneCir.InsideColor = _flankCir.InsideColor = _rearCir.InsideColor = right;

            SubItems = new IDrawing3D[] { _noneCir, _flankCir, _rearCir };
        }

        public override void UpdateOnFrame(XIVPainter.XIVPainter painter)
        {
            var pos = Positional;
            if (!Target.HasPositional() || Player.Available && Player.Object.HasStatus(true, CustomRotation.TrueNorth.StatusProvide))
            {
                pos = EnemyPositional.None;
            }
            switch (pos)
            {
                case EnemyPositional.Flank:
                    _flankCir.Target = Target;
                    _rearCir.Target = null;
                    _noneCir.Target = null;
                    break;

                case EnemyPositional.Rear:
                    _flankCir.Target = null;
                    _rearCir.Target = Target;
                    _noneCir.Target = null;
                    break;

                default:
                    _flankCir.Target = null;
                    _rearCir.Target = null;
                    _noneCir.Target = Target;
                    break;
            }
            base.UpdateOnFrame(painter);
        }
    }

    static XIVPainter.XIVPainter _painter;
    static PositionalDrawing _positional;
    static Drawing3DAnnulusFO _annulus;

    public static void Init()
    {
        _painter = Svc.PluginInterface.Create<XIVPainter.XIVPainter>("RotationSolverOverlay");
        _painter.UseTaskForAccelerate = false;

        _annulus = new Drawing3DAnnulusFO(Player.Object, 3, 3 + Service.Config.MeleeRangeOffset,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.8f, 0.75f, 0.15f)), 2);
        _annulus.InsideColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.3f, 0.2f, 0.15f));

        _annulus.UpdateEveryFrame = () =>
        {
            if (Player.Available && (Player.Object.IsJobCategory(JobRole.Tank) || Player.Object.IsJobCategory(JobRole.Melee)) && (Svc.Targets.Target?.IsNPCEnemy() ?? false) && Service.Config.DrawMeleeOffset
            && DataCenter.StateType != StateCommandType.Cancel)
            {
                _annulus.Target = Svc.Targets.Target;
            }
            else
            {
                _annulus.Target = null;
            }
        };

        _positional = new PositionalDrawing();

        _painter.AddDrawings(_positional, _annulus);
        //_painter.AddDrawings(new Drawing3DCircularSectorFO(Player.Object, 3, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.5f, 0.4f, 0.15f)), 5));
    }

    public static void UpdatePositional(EnemyPositional positional, GameObject target)
    {
        _positional.Target = target;
        _positional.Positional = positional;
    }

    public static void ClearPositional()
    {
        _positional.Target = null;
    }

    public static void Dispose()
    {
        _painter?.Dispose();
    }
}
