using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Storex;

namespace RepositoryModules
{
    [RepositoryModuleExport("E0B3F83A-B417-43DB-8CCF-9916A2EB63C6", "簡易ファイルシステム [C#]", Description = "モードでシンボル内容等を設定します")]
    public class RepositoryModule1 : IRepositoryModule
    {
        static readonly Encoding _textEncoding = Encoding.GetEncoding("shift_jis");

        IMode _currentMode;
        IUser _currentUser;

        public bool IsConfiguable { get; private set; } = true;

        public bool IsModeConfiguable { get; private set; } = true;

        public bool CanConfigureMode => true;

        public Task ConfigureAsync()
        {
            ConfigForm1.Configure();

            return Task.CompletedTask;
        }

        public Task ConfigureModeAsync(IMode mode)
        {
            ModeConfigForm1.Configure(mode);

            return Task.CompletedTask;
        }

        public Task PrepareAsync(IMode Mode, IUser UserInfo)
        {
            if (!File.Exists(MySettings.Default.FilePath))
            {
                throw new RepositoryModuleException("概要データ ファイルが正しく設定されていません。");
            }

            if (!Directory.Exists(MySettings.Default.FolderPath))
            {
                throw new RepositoryModuleException("詳細データ フォルダが正しく設定されていません。");
            }

            if (!Mode.TryExtractProperty<FixedLengthSpec>(FixedLengthSpec.PropertyKeyForPrimary, out var _priaryLabelFixedLengthSpec) |
                !Mode.TryExtractProperty<SecondaryLabelCriteria>(SecondaryLabelCriteria.PropertyKey, out var _secondaryLabelCriteria))
            {
                throw new RepositoryModuleException("モードが構成されていません。");
            }

            _currentMode = Mode;
            _currentUser = UserInfo;

            return Task.CompletedTask;
        }

        FixedLengthSpec _primaryLabelSpec;

        public ILabel FindPrimaryLabel(ILabelSource[] sources)
        {
            var labels = sources
                .OfType<Symbol>()
                .Select(x => _primaryLabelSpec.TryGeneraLabel(x, out var label) ? label : null)
                .Where(x => x != null)
                .ToArray();

            return labels.Length == 1 ? labels[0] : null;
        }

        SecondaryLabelCriteria _secondaryLabelCriteria;

        public ILabel[] FindSecondaryLabels(ILabel primary, ILabelSource[] sources)
        {
            var labels = sources
                .OfType<C3Label>()
                .Where(x => x.PartNumber == primary.ItemNumber)
                .ToArray();

            return labels;
        }

        public Task RegisterAsync(ILabel primary, ILabel secondary, CaptureData[] captureDatas, string[] tags)
        {
            return RegisterAsync(primary, secondary, captureDatas, tags, CancellationToken.None);
        }

        public async Task RegisterAsync(ILabel primary, ILabel secondary, CaptureData[] captureDatas, string[] tags, CancellationToken cancellationToken)
        {
            if (_secondaryLabelCriteria.IsRequired & secondary is null)
                throw new RepositoryModuleException("C-3 ラベルが必要です。");

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

                File.WriteAllBytes($@"{captureDataFolder.FullName}\original.jpg", (byte[])captureData.OriginalImage);

                if (captureData.AdornedImage != null)
                {
                    File.WriteAllBytes($@"{captureDataFolder.FullName}\adorned.jpeg", (byte[])captureData.AdornedImage);
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
        }

        public void Dispose()
        {
        }
    }
}