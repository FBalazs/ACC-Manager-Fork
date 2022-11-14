﻿using ACCManager.Data.ACC.Database.LapDataDB;
using ACCManager.Data.ACC.Tracker.Laps;
using ACCManager.HUD.Overlay.Configuration;
using ACCManager.HUD.Overlay.Internal;
using ACCManager.HUD.Overlay.OverlayUtil;
using System;
using System.Drawing;
using System.Drawing.Text;

namespace ACCManager.HUD.ACC.Overlays.OverlayLapDelta
{
    [Overlay(Name = "Lap Info", Version = 1.00, OverlayType = OverlayType.Release,
        Description = "A panel with a bar showing the current delta.\nOptionally showing the sector times, last lap, best lap and the potential best.")]
    internal sealed class LapInfoOverlay : AbstractOverlay
    {
        private readonly LapInfoConfig _config = new LapInfoConfig();
        private class LapInfoConfig : OverlayConfiguration
        {
            [ToolTip("Displays the time for each sector, green colored sectors are personal best.")]
            public bool ShowSectors { get; set; } = true;

            [ToolTip("Displays the last lap time.")]
            public bool ShowLastLap { get; set; } = true;

            [ToolTip("Displays the best lap time.")]
            public bool ShowBestLap { get; set; } = true;

            [ToolTip("Displays the potential best lap time based on your fastest sector times.")]
            public bool ShowPotentialBest { get; set; } = true;

            [ToolTip("Sets the maximum range in seconds for the delta bar.")]
            [IntRange(1, 5, 1)]
            public int MaxDelta { get; set; } = 2;

            public LapInfoConfig() : base()
            {
                this.AllowRescale = true;
            }
        }

        private const int _overlayWidth = 202;
        private readonly InfoTable _table;

        private DbLapData _lastLap = null;

        public LapInfoOverlay(Rectangle rectangle) : base(rectangle, "Lap Info Overlay")
        {
            _table = new InfoTable(10, new int[] { 85, 83 }) { Y = 17 };
            this.Width = _overlayWidth + 1;
            this.Height = _table.FontHeight * 7 + 2 + 4;
        }

        public sealed override void BeforeStart()
        {
            if (!this._config.ShowSectors)
                this.Height -= this._table.FontHeight * 3;

            if (!this._config.ShowPotentialBest)
                this.Height -= this._table.FontHeight;

            if (!this._config.ShowLastLap)
                this.Height -= this._table.FontHeight;

            if (!this._config.ShowBestLap)
                this.Height -= this._table.FontHeight;

            LapTracker.Instance.LapFinished += Collector_LapFinished;
        }

        public sealed override void BeforeStop()
        {
            LapTracker.Instance.LapFinished -= Collector_LapFinished;
        }

        private void Collector_LapFinished(object sender, DbLapData newLap)
        {
            if (newLap.Sector1 != -1 && newLap.Sector2 != -1 && newLap.Sector3 != -1)
                _lastLap = newLap;
        }

        public sealed override void Render(Graphics g)
        {
            double delta = (double)pageGraphics.DeltaLapTimeMillis / 1000;
            DeltaBar deltaBar = new DeltaBar(-this._config.MaxDelta, this._config.MaxDelta, delta) { DrawBackground = true, IsValidLap = pageGraphics.IsValidLap };
            deltaBar.Draw(g, 0, 0, _overlayWidth, _table.FontHeight);

            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.TextContrast = 1;

            string deltaText = $"{delta:F3}";
            SizeF textWidth = g.MeasureString(deltaText, _table.Font);
            g.DrawStringWithShadow(deltaText, _table.Font, Color.White, new PointF(_overlayWidth / 2 - textWidth.Width + textWidth.Width / 2, _table.FontHeight / 8f));

            if (this._config.ShowSectors)
                AddSectorLines();

            if (this._config.ShowLastLap)
                AddLastLap();

            if (this._config.ShowBestLap)
                AddBestLap();

            if (this._config.ShowPotentialBest)
                AddPotentialBest();

            _table.Draw(g);
        }

        private void AddPotentialBest()
        {
            string[] potentialValues = new string[2];

            int potentialBest = LapTracker.Instance.Laps.GetPotentialFastestLapTime();
            if (potentialBest == -1)
                potentialValues[0] = $"--:--.---";
            else
            {
                TimeSpan best = TimeSpan.FromMilliseconds(potentialBest);
                potentialValues[0] = $"{best:mm\\:ss\\:fff}";
            }

            this._table.AddRow("Pot", potentialValues);
        }

        private void AddSectorLines()
        {
            DbLapData lap = LapTracker.Instance.CurrentLap;

            if (_lastLap != null && pageGraphics.NormalizedCarPosition < 0.08 && lap.Index != _lastLap.Index && _lastLap.Sector3 != -1)
                lap = _lastLap;

            int fastestSector1 = LapTracker.Instance.Laps.GetFastestSector(1);
            int fastestSector2 = LapTracker.Instance.Laps.GetFastestSector(2);
            int fastestSector3 = LapTracker.Instance.Laps.GetFastestSector(3);

            string[] rowSector1 = new string[2];
            string[] rowSector2 = new string[2];
            string[] rowSector3 = new string[2];
            rowSector1[0] = "-";
            rowSector2[0] = "-";
            rowSector3[0] = "-";

            if (LapTracker.Instance.CurrentLap.Sector1 > -1)
            {
                rowSector1[0] = $"{lap.GetSector1():F3}";
                if (lap.Sector1 > fastestSector1)
                    rowSector1[1] = $"+{(float)(lap.Sector1 - fastestSector1) / 1000:F3}";
            }
            else if (pageGraphics.CurrentSectorIndex == 0)
                rowSector1[0] = $"{((float)pageGraphics.CurrentTimeMs / 1000):F3}";


            if (lap.Sector2 > -1)
            {
                rowSector2[0] = $"{lap.GetSector2():F3}";
                if (lap.Sector2 > fastestSector2)
                    rowSector2[1] = $"+{(float)(lap.Sector2 - fastestSector2) / 1000:F3}";
            }
            else if (lap.Sector1 > -1)
            {
                rowSector2[0] = $"{(((float)pageGraphics.CurrentTimeMs - lap.Sector1) / 1000):F3}";
            }

            if (lap.Sector3 > -1)
            {
                rowSector3[0] = $"{lap.GetSector3():F3}";
                if (lap.Sector3 > fastestSector3)
                    rowSector3[1] = $"+{(float)(lap.Sector3 - fastestSector3) / 1000:F3}";
            }
            else if (lap.Sector2 > -1 && pageGraphics.CurrentSectorIndex == 2)
            {
                rowSector3[0] = $"{(((float)pageGraphics.CurrentTimeMs - lap.Sector2 - lap.Sector1) / 1000):F3}";
            }


            if (pageGraphics.CurrentSectorIndex != 0 && lap.Sector1 != -1 && lap.IsValid)
                _table.AddRow("S1 ", rowSector1, new Color[] { LapTracker.Instance.Laps.IsSectorFastest(1, lap.Sector1) ? Color.LimeGreen : Color.White, Color.Orange });
            else
                _table.AddRow("S1 ", rowSector1, new Color[] { Color.White });

            if (pageGraphics.CurrentSectorIndex != 1 && lap.Sector2 != -1 && lap.IsValid)
                _table.AddRow("S2 ", rowSector2, new Color[] { LapTracker.Instance.Laps.IsSectorFastest(2, lap.Sector2) ? Color.LimeGreen : Color.White, Color.Orange });
            else
                _table.AddRow("S2 ", rowSector2, new Color[] { Color.White });

            if (pageGraphics.CurrentSectorIndex != 2 && lap.Sector3 != -1 && lap.IsValid)
                _table.AddRow("S3 ", rowSector3, new Color[] { LapTracker.Instance.Laps.IsSectorFastest(3, lap.Sector3) ? Color.LimeGreen : Color.White, Color.Orange });
            else
                _table.AddRow("S3 ", rowSector3, new Color[] { Color.White });
        }

        private void AddLastLap()
        {
            string[] LastLapValues = new string[2];

            int lastLap = LapTracker.Instance.Laps.GetLastLapTime();
            if (lastLap == -1)
                LastLapValues[0] = $"--:--.---";
            else
            {
                TimeSpan best = TimeSpan.FromMilliseconds(lastLap);
                LastLapValues[0] = $"{best:mm\\:ss\\:fff}";
            }

            this._table.AddRow("Last", LastLapValues);
        }

        private void AddBestLap()
        {
            string[] bestLapValues = new string[2];

            int bestLap = LapTracker.Instance.Laps.GetBestLapTime();
            if (bestLap == -1)
                bestLapValues[0] = $"--:--.---";
            else
            {
                TimeSpan best = TimeSpan.FromMilliseconds(bestLap);
                bestLapValues[0] = $"{best:mm\\:ss\\:fff}";
            }

            if (broadCastLocalCar.CarIndex == broadCastRealTime.BestLapCarIndex)
                this._table.AddRow("Best", bestLapValues, new Color[] { Color.Purple });
            else
                this._table.AddRow("Best", bestLapValues);
        }

        public sealed override bool ShouldRender()
        {
            return DefaultShouldRender();
        }
    }
}
