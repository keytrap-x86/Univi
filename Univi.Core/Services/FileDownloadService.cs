using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Univi.Core.Services
{
    public class FileDownloadService
    {

        public async Task<string?> DownloadFileWithProgressAsync(string url, string destinationPath, Func<double, double> progress = null, CancellationToken token = default)
        {
            using var httpClient = new HttpClient();

            // Effectuez la requête et obtenez la réponse avec les en-têtes uniquement (sans lire tout le contenu)
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

            // Vérifiez si la réponse est réussie et lancez une exception si ce n'est pas le cas
            response.EnsureSuccessStatusCode();

            // Obtenez la taille totale du fichier à partir de l'en-tête Content-Length
            var totalFileSize = response.Content.Headers.ContentLength.GetValueOrDefault(0);

            // Obtenez le nom du fichier à partir de l'URL
            var fileName = Path.GetFileName(new Uri(url).LocalPath);

            // Créez le chemin de destination complet
            destinationPath = Path.Combine(destinationPath, fileName);

            // Créez un flux pour écrire le fichier téléchargé
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

            // Obtenez le flux de contenu de la réponse
            using var contentStream = await response.Content.ReadAsStreamAsync(token);

            var totalBytesRead = 0L;
            var buffer = new byte[8192];
            var isReading = true;

            do
            {
                // Lisez les données du flux de contenu dans le tampon
                var bytesRead = await contentStream.ReadAsync(buffer, token);

                if (bytesRead == 0)
                {
                    isReading = false;
                    break;
                }

                // Écrivez les données dans le fichier
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);

                totalBytesRead += bytesRead;

                // Calculez la progression en pourcentage
                var progressPercentage = (int)((totalBytesRead / (double)totalFileSize) * 100);

                progress?.Invoke(progressPercentage);

            } while (isReading);

            return destinationPath;
        }
    }
}
