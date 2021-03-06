﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using SaintCoinach.Graphics;
using SaintCoinach.Graphics.Viewer;
using SaintCoinach.Graphics.Viewer.Content;
using SaintCoinach.Xiv;

namespace Godbert.ViewModels {
    using Commands;

    public class MonstersViewModel : ObservableBase {
        const string ImcPathFormat = "chara/monster/m{0:D4}/obj/body/b{1:D4}/b{1:D4}.imc";
        const string ModelPathFormat = "chara/monster/m{0:D4}/obj/body/b{1:D4}/model/m{0:D4}b{1:D4}.mdl";

        #region Fields
        private Models.ModelCharaHierarchy _Entries;
        private object _SelectedEntry;
        #endregion

        #region Properties
        public Models.ModelCharaHierarchy Entries {
            get { return _Entries; }
            private set {
                _Entries = value;
                OnPropertyChanged(() => Entries);
            }
        }
        public object SelectedEntry {
            get { return _SelectedEntry; }
            set {
                _SelectedEntry = value;
                OnPropertyChanged(() => SelectedEntry);
                OnPropertyChanged(() => IsValidSelection);
            }
        }
        public bool IsValidSelection { get { return SelectedEntry is Models.ModelCharaVariant; } }
        public MainViewModel Parent { get; private set; }
        #endregion

        #region Constructor
        public MonstersViewModel(MainViewModel parent) {
            this.Parent = parent;

            var modelCharaSheet = Parent.Realm.GameData.GetSheet<ModelChara>();

            Entries = new Models.ModelCharaHierarchy("m{0:D4}", "b{0:D4}", "v{0:D4}");
            foreach(var mc in modelCharaSheet.Where(mc => mc.Type == 3)) {
                var imcPath = string.Format(ImcPathFormat, mc.ModelKey, mc.BaseKey);
                var mdlPath = string.Format(ModelPathFormat, mc.ModelKey, mc.BaseKey);
                if(!Parent.Realm.Packs.FileExists(imcPath) ||!Parent.Realm.Packs.FileExists(mdlPath))
                    continue;

                Entries.Add(mc);
            }
        }
        #endregion

        #region Command
        private ICommand _AddCommand;
        private ICommand _ReplaceCommand;
        private ICommand _NewCommand;

        public ICommand AddCommand { get { return _AddCommand ?? (_AddCommand = new DelegateCommand(OnAdd)); } }
        public ICommand ReplaceCommand { get { return _ReplaceCommand ?? (_ReplaceCommand = new DelegateCommand(OnReplace)); } }
        public ICommand NewCommand { get { return _NewCommand ?? (_NewCommand = new DelegateCommand(OnNew)); } }

        private void OnAdd() {
            ModelDefinition model;
            ImcVariant variant;
            if (TryGetModel(out model, out variant))
                Parent.EngineHelper.AddToLast(SelectedEntry.ToString(), (e) => new SaintCoinach.Graphics.Viewer.Content.ContentModel(e, variant, model, ModelQuality.High));
        }
        private void OnReplace() {
            ModelDefinition model;
            ImcVariant variant;
            if (TryGetModel(out model, out variant))
                Parent.EngineHelper.ReplaceInLast(SelectedEntry.ToString(), (e) => new SaintCoinach.Graphics.Viewer.Content.ContentModel(e, variant, model, ModelQuality.High));
        }
        private void OnNew() {
            ModelDefinition model;
            ImcVariant variant;
            if (TryGetModel(out model, out variant))
                Parent.EngineHelper.OpenInNew(SelectedEntry.ToString(), (e) => new SaintCoinach.Graphics.Viewer.Content.ContentModel(e, variant, model, ModelQuality.High));
        }

        private bool TryGetModel(out ModelDefinition model, out ImcVariant variant) {
            model = null;
            variant = ImcVariant.Default;

            var asVariant = SelectedEntry as Models.ModelCharaVariant;
            if (asVariant == null)
                return false;

            int v = asVariant.Value;
            int b = asVariant.Parent.Value;
            var m = asVariant.Parent.Parent.Value;

            var imcPath = string.Format(ImcPathFormat, m, b);
            var mdlPath = string.Format(ModelPathFormat, m, b);

            SaintCoinach.IO.File imcFileBase;
            SaintCoinach.IO.File mdlFileBase;
            if (!Parent.Realm.Packs.TryGetFile(imcPath, out imcFileBase) || !Parent.Realm.Packs.TryGetFile(mdlPath, out mdlFileBase) || !(mdlFileBase is ModelFile)) {
                System.Windows.MessageBox.Show(string.Format("Unable to find files for {0}.", asVariant), "File not found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            try {
                var imcFile = new ImcFile(imcFileBase);
                model = ((ModelFile)mdlFileBase).GetModelDefinition();
                variant = imcFile.GetVariant(v);

                return true;
            } catch (Exception e) {
                System.Windows.MessageBox.Show(string.Format("Unable to load model for {0}:{1}{2}", asVariant, Environment.NewLine, e), "Failure to load", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }
        #endregion

        #region Brute-force
        private bool _IsBruteForceAvailable = true;
        private ICommand _BruteForceCommand;

        public bool IsBruteForceAvailable {
            get { return _IsBruteForceAvailable; }
            private set {
                _IsBruteForceAvailable = value;
                OnPropertyChanged(() => IsBruteForceAvailable);
            }
        }
        public ICommand BruteForceCommand { get { return _BruteForceCommand ?? (_BruteForceCommand = new DelegateCommand(OnBruteForce)); } }

        private void OnBruteForce() {
            IsBruteForceAvailable = false;

            var progDlg = new Ookii.Dialogs.Wpf.ProgressDialog();
            progDlg.WindowTitle = "Brute-forcing";
            progDlg.Text = "This is going to take a while...";
            progDlg.DoWork += DoBruteForceWork;
            progDlg.RunWorkerCompleted += OnBruteForceComplete;
            progDlg.ShowDialog(System.Windows.Application.Current.MainWindow);
            progDlg.ProgressBarStyle = Ookii.Dialogs.Wpf.ProgressBarStyle.ProgressBar;
            progDlg.ShowTimeRemaining = true;
        }

        void OnBruteForceComplete(object sender, System.ComponentModel.RunWorkerCompletedEventArgs eventArgs) {
            if (eventArgs.Cancelled)
                IsBruteForceAvailable = true;
        }

        void DoBruteForceWork(object sender, System.ComponentModel.DoWorkEventArgs eventArgs) {
            var dlg = (Ookii.Dialogs.Wpf.ProgressDialog)sender;

            var newEntries = new Models.ModelCharaHierarchy(Entries.MainFormat, Entries.SubFormat, Entries.VariantFormat);
            for (var m = 0; m < 10000; ++m) {
                if (dlg.CancellationPending)
                    return;
                dlg.ReportProgress(m / 100, null, string.Format("Current progress: {0:P}", m / 10000.0));
                for (var b = 0; b < 10000; ++b) {

                    var imcPath = string.Format(ImcPathFormat, m, b);
                    SaintCoinach.IO.File imcBase;
                    if (!Parent.Realm.Packs.TryGetFile(imcPath, out imcBase))
                        continue;
                    try {
                        var imc = new SaintCoinach.Graphics.ImcFile(imcBase);
                        for (var v = 1; v < imc.Count; ++v) {
                            if (Entries.Contains(m, b, v))
                                continue;

                            var any = false;
                            foreach (var p in imc.Parts) {
                                if (p.Variants[v].Variant != 0) {
                                    any = true;
                                    break;
                                }
                            }
                            if (any)
                                newEntries.Add(m, b, v);
                        }
                    } catch (Exception ex) {
                        Console.Error.WriteLine("Failed parsing imc file {0}:{1}{2}", imcPath, Environment.NewLine, ex);
                    }
                }
            }
            Entries = newEntries;
        }
        #endregion
    }
}
