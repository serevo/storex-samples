﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Storex;

namespace RepositoryModules
{
    using static RepositoryModuleHelper;

    [RepositoryModuleExport("E0B3F83A-B417-43DB-8CCF-9916A2EB63C6", "簡易ファイルシステム", Description = "モードでシンボル内容等を設定します")]
    public class RepositoryModule1 : IRepositoryModule
    {
        static readonly Encoding _textEncoding = Encoding.GetEncoding("shift_jis");

        IMode _currentMode;
        IUser _currentUser;

        public bool IsConfiguable { get; private set; } = true;

        public bool IsModeConfiguable { get; private set; } = true;

        public bool CanConfigureMode => true;

        public Task ConfigureAsync(CancellationToken cancellationToken)
        {
            ConfigForm1.Configure();

            return Task.CompletedTask;
        }

        public Task ConfigureModeAsync(IMode mode, CancellationToken cancellationToken)
        {
            ModeConfigForm1.Configure(mode);

            return Task.CompletedTask;
        }

        public Task<bool> PrepareAsync(IMode Mode, IUser UserInfo, CancellationToken cancellationToken)
        {
            if (!File.Exists(MySettings.Default.FilePath))
            {
                throw new RepositoryModuleException("概要データ ファイルが正しく設定されていません。");
            }

            if (!Directory.Exists(MySettings.Default.FolderPath))
            {
                throw new RepositoryModuleException("詳細データ フォルダが正しく設定されていません。");
            }

            if (!Mode.TryExtractProperty(FixedLengthSpec.PropertyKeyForPrimary, out _priaryLabelFixedLengthSpec) |
                !Mode.TryExtractProperty(SecondaryLabelCriteria.PropertyKey, out _secondaryLabelCriteria))
            {
                throw new RepositoryModuleException("モードが構成されていません。");
            }

            _conditionDictionary = ReadSecondaryConditionsFile();

            _currentMode = Mode;
            _currentUser = UserInfo;

            return Task.FromResult(true);
        }

        FixedLengthSpec _priaryLabelFixedLengthSpec;

        public Task<ILabel> FindPrimaryLabelAsync(ILabelSource[] sources, CancellationToken cancellationToken)
        {
            var labels = sources
                .OfType<Symbol>()
                .Select(x => _priaryLabelFixedLengthSpec.TryGenerateLabel(x, out var label) ? label : null)
                .Where(x => x != null)
                .ToArray();

            return Task.FromResult(labels.Length == 1 ? labels[0] : null);
        }

        SecondaryLabelCriteria _secondaryLabelCriteria;
        IReadOnlyDictionary<string, IEnumerable<string>> _conditionDictionary;

        public Task<ILabel[]> FindSecondaryLabelsAsync(ILabel primary, ILabelSource[] sources, CancellationToken cancellationToken)
        {
            var labels = new List<ILabel>();

            var validConditionValues = 
                _secondaryLabelCriteria.SpecifiedByConditionFileBehavior != SecondaryLabelBehavior.Deny
                    &&  _conditionDictionary.TryGetValue(primary.ItemNumber, out var strings) ? 
                strings :
                Array.Empty<string>();

            var c3Labels = sources
                .Where(_ => (_secondaryLabelCriteria.AcceptableTypes & SecondaryLabelTypes.C3Label) == SecondaryLabelTypes.C3Label)
                .OfType<C3Label>()
                .Where(o =>
                    (_secondaryLabelCriteria.ItemNumberEqualsToPrimaryOneBehavior != SecondaryLabelBehavior.Deny && EqualsIgnoreCase(o.PartNumber, primary.ItemNumber))
                    | (_secondaryLabelCriteria.SpecifiedByConditionFileBehavior != SecondaryLabelBehavior.Deny && validConditionValues.Any(p => EqualsIgnoreCase(o.PartNumber, p)))
                    | (_secondaryLabelCriteria.OtherNotSingleSymbolLabelsBehavior != SecondaryLabelBehavior.Deny && !EqualsIgnoreCase(o.PartNumber, primary.ItemNumber) & !validConditionValues.Any(p => EqualsIgnoreCase(o.PartNumber, p)))
                    )
                .ToList();

            labels.AddRange(c3Labels);

            var singleSymbolLabels = sources
                .Where(_ => (_secondaryLabelCriteria.AcceptableTypes & SecondaryLabelTypes.SingleSymbol) == SecondaryLabelTypes.SingleSymbol)
                .OfType<Symbol>()
                .Where(x => !_priaryLabelFixedLengthSpec.TryGenerateLabel(x, out _))
                .Select(o => new BasicLabel(o) { ItemNumber = o.Value })
                .Where(o =>
                    (_secondaryLabelCriteria.ItemNumberEqualsToPrimaryOneBehavior != SecondaryLabelBehavior.Deny & EqualsIgnoreCase(o.ItemNumber, primary.ItemNumber))
                    | (_secondaryLabelCriteria.SpecifiedByConditionFileBehavior != SecondaryLabelBehavior.Deny & validConditionValues.Any(p => EqualsIgnoreCase(o.ItemNumber, p)))
                    )
                .ToList();

            labels.AddRange(singleSymbolLabels);

            return Task.FromResult(labels.ToArray());
        }

        public async Task<bool> RegisterAsync(ILabel primary, ILabel secondary, CaptureData[] captureDatas, string[] tags, CancellationToken cancellationToken)
        {
            if (!File.Exists(MySettings.Default.FilePath))
            {
                throw new RepositoryModuleException("概要データ ファイルが存在しません。");
            }

            if (!Directory.Exists(MySettings.Default.FolderPath))
            {
                throw new RepositoryModuleException("詳細データ フォルダが存在しません。");
            }

            if (secondary is null)
            {
                if (_secondaryLabelCriteria.NoLabelBehavior == SecondaryNoLabelBehavior.Warnning
                    && !ShowAlert("セカンダリ ラベルが必要です。無視して登録しますか。"))
                {
                    return false;
                }
                else if (_secondaryLabelCriteria.NoLabelBehavior == SecondaryNoLabelBehavior.Deny)
                {
                    ShowError("セカンダリ ラベルが必要です。");
                    return false;
                }
                else if (_secondaryLabelCriteria.NoLabelBehavior != SecondaryNoLabelBehavior.Default)
                {
                    var hasItemNumberEqualsToPrimary = tags.Any(o => EqualsIgnoreCase(o, primary.ItemNumber));
                    var hasSpecifiedByConditionFile = 
                        tags.Any(o => _conditionDictionary.TryGetValue(primary.ItemNumber, out var strings) && strings.Any(p => EqualsIgnoreCase(o, p)));

                    if (_secondaryLabelCriteria.NoLabelBehavior == SecondaryNoLabelBehavior.WarningWhenTagNotMatched
                        && !hasItemNumberEqualsToPrimary && !hasSpecifiedByConditionFile
                        && !ShowAlert("セカンダリ ラベル、又は対応するタグが必要です。無視して登録しますか。"))
                    {
                        return false;
                    }
                    else if (_secondaryLabelCriteria.NoLabelBehavior == SecondaryNoLabelBehavior.DenyWhenTagNotMatched
                        && !hasItemNumberEqualsToPrimary && !hasSpecifiedByConditionFile)
                    {
                        ShowError("セカンダリ ラベル、又は対応するタグが必要です。");
                        return false;
                    }
                    else if (_secondaryLabelCriteria.ItemNumberEqualsToPrimaryOneBehavior == SecondaryLabelBehavior.Warnning
                        && hasItemNumberEqualsToPrimary
                        && !ShowAlert("プライマリラベルと同じ品番がタグに含まれています。登録しますか。"))
                    {
                        return false;
                    }
                    else if (_secondaryLabelCriteria.SpecifiedByConditionFileBehavior == SecondaryLabelBehavior.Warnning
                        && hasSpecifiedByConditionFile
                        && !ShowAlert("対応表で指定されている品番がタグに含まれています。登録しますか。"))
                    {
                        return false;
                    }
                }
            }
            else if (_secondaryLabelCriteria.ItemNumberEqualsToPrimaryOneBehavior == SecondaryLabelBehavior.Warnning
                && EqualsIgnoreCase(primary.ItemNumber, secondary.ItemNumber)
                && !ShowAlert("指定された セカンダリ ラベルは、プライマリラベルと同じ品番です。登録を続行しますか。"))
            {
                return false;
            }
            else if (_secondaryLabelCriteria.SpecifiedByConditionFileBehavior == SecondaryLabelBehavior.Warnning
                && _conditionDictionary.TryGetValue(primary.ItemNumber, out var strings1) 
                && strings1.Any(o => EqualsIgnoreCase(o, secondary.ItemNumber))
                && !ShowAlert("指定された セカンダリ ラベルは、プライマリ ラベルとは異なり対応表と同じ品番です。登録を続行しますか。"))
            {
                return false;
            }
            else if (_secondaryLabelCriteria.OtherNotSingleSymbolLabelsBehavior == SecondaryLabelBehavior.Warnning
                && !EqualsIgnoreCase(primary.ItemNumber, secondary.ItemNumber)
                && !(_conditionDictionary.TryGetValue(primary.ItemNumber, out var strings2) && strings2.Any(o => EqualsIgnoreCase(o, secondary.ItemNumber)))
                && !ShowAlert("指定された セカンダリ ラベルは、プライマリラベルや対応表と異なる品番です。登録しますか。"))
            {
                return false;
            }

            var timestamp = DateTime.Now;

            var myFile = new FileInfo(MySettings.Default.FilePath);

            using (var sw = new StreamWriter(myFile.FullName, true, _textEncoding))
            {
                if (myFile.Length == 0L)
                {
                    await sw.WriteLineAsync(string.Join("\t", new string[]
                    {
                        "PrimaryPartNumber",
                        "PrimarySerialNumber",
                        "SecondaryPartNumber",
                        "SecondarySerialNumber",
                        "ModeId",
                        "UserName",
                        "Timestamp",
                    }));
                }

                await sw.WriteLineAsync(string.Join("\t", new string[]
                {
                    primary.ItemNumber,
                    primary.SerialNumber,
                    secondary?.ItemNumber,
                    secondary?.SerialNumber,
                    _currentMode.Id.ToString(),
                    _currentUser?.DisplayName,
                    $"{timestamp:yyyy/MM/dd HH:mm:ss.fff}",
                }));
            }

            var mFolder = new DirectoryInfo(MySettings.Default.FolderPath);

            mFolder = mFolder.CreateSubdirectory($"{timestamp:yyyy-MM-dd HH-mm-ss.fff}");

            for (int i = 1, loopTo = captureDatas.Count(); i <= loopTo; i++)
            {
                var captureData = captureDatas[i - 1];

                var captureDataFolder = mFolder.CreateSubdirectory($"catpure#{i}");

                WriteImage($@"{captureDataFolder.FullName}\original.jpg", captureData.OriginalImage.ToArray());

                if (captureData.AdornedImage != null)
                {
                    WriteImage($@"{captureDataFolder.FullName}\adorned.jpeg", captureData.AdornedImage.ToArray());
                }

                if (captureData.LabelSources.Count > 0)
                {
                    var symbolValues = captureData.LabelSources.SelectMany(x => x.Symbols.Select(xx => xx.Value)).ToArray();

                    File.WriteAllLines($@"{captureDataFolder.FullName}\symbols.txt", symbolValues);
                }
            }

            if (tags.Length > 0)
            {
                File.WriteAllLines($@"{mFolder.FullName}\tags.txt", tags);
            }

            return true;
        }

        public void Dispose() { }
    }
}