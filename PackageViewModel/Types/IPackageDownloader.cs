using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGetPe;

namespace NuGetPackageExplorer.Types
{
    public interface INuGetPackageDownloader
    {
        Task<ISignaturePackage?> Download(SourceRepository sourceRepository, PackageIdentity packageIdentity);
        Task<ISignaturePackage?> Download(SourceRepository sourceRepository, PackageIdentity packageIdentity, CancellationToken ct,  IProgress<(long ReceivedBytes, long? TotalBytes)>? progress);

        Task Download(string targetFilePath, SourceRepository sourceRepository, PackageIdentity packageIdentity);
    }
}
