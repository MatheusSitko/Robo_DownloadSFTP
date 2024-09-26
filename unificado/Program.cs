using Renci.SshNet;
using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Amazon.S3.Transfer;

class SftpRobot
{
    private static string ConnectionString = "Server=;Port=;Database=;User ID=;Password=;";
    private class SftpCredentials
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = //ID port;
    }

    private static List<SftpCredentials> sftpCredentialsList = new List<SftpCredentials>
    {
        new SftpCredentials { Host = "", Username = "", Password = "" },
        new SftpCredentials { Host = "", Username = "", Password = "" },
        new SftpCredentials { Host = "", Username = "", Password = "" },
        new SftpCredentials { Host = "", Username = "", Password = "" },
        new SftpCredentials { Host = "", Username = "", Password = "" },
        new SftpCredentials { Host = "", Username = "", Password = "" }
    };
    public static async Task Main(string[] args)
    {
        IDbConnection dbConnection = new MySqlConnection(ConnectionString);

        // Configuração do S3
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        
        foreach (var credentials in sftpCredentialsList)
        {
            Console.WriteLine($"Iniciando conexão com {credentials.Username}...");

            using (var sftp = new SftpClient(credentials.Host, credentials.Port, credentials.Username, credentials.Password))
            {
                try
                {
                    sftp.Connect();
                    Console.WriteLine($"Conectado ao SFTP {credentials.Username}.");

                    string remoteDirectory = "/Remote Directory";
                    var files = sftp.ListDirectory(remoteDirectory);

                    Console.WriteLine("Arquivos no diretório remoto:");

                    var today = DateTime.Today;
                    var todayFiles = files.Where(f => f.LastWriteTime.Date == today && !f.IsDirectory).ToList();

                    if (!todayFiles.Any())
                    {
                        Console.WriteLine("O diretório informado não possui arquivos do dia.");
                        continue;
                    }

                    var serviceCollection = new ServiceCollection();
                    serviceCollection.AddSingleton(configuration);
                    serviceCollection.AddDefaultAWSOptions(configuration.GetAWSOptions());
                    serviceCollection.AddAWSService<IAmazonS3>();
                    var serviceProvider = serviceCollection.BuildServiceProvider();
                    var s3Client = serviceProvider.GetService<IAmazonS3>();
                    var fileTransferUtility = new TransferUtility(s3Client);

                    foreach (var file in todayFiles)
                    {
                        Console.WriteLine($"Processando o arquivo: {file.Name}");

                        using (var memoryStream = new MemoryStream())
                        {                           
                            sftp.DownloadFile(file.FullName, memoryStream);
                            memoryStream.Position = 0; // Volta para o início do stream para o upload

                            // Define o diretório de destino no S3 com base no nome do arquivo
                            string keyPrefix = GetS3KeyPrefix(file.Name);
                                                        
                            var todayDatePath = $"{DateTime.Now:yyyy/MM/dd}";

                            // Diretório completo no formato desejado
                            var fullPath = $"teste-upload/{todayDatePath}/{keyPrefix}/{file.Name}";

                            var bucketName = "BucketName";
                                                        
                            Console.WriteLine($"Verificando diretório no S3: {fullPath}");
                            var fileSize = memoryStream.Length;

                            // Envia o arquivo para o S3
                            await fileTransferUtility.UploadAsync(memoryStream, bucketName, fullPath);
                            Console.WriteLine($"Arquivo enviado para o S3: {fullPath}");
                                                        
                            var startTime = DateTime.Now;
                            var id = await LogDownloadStartAsync(dbConnection, credentials.Username, file.Name, startTime);
                            var endTime = DateTime.Now;
                            
                            await LogDownloadEndAsync(dbConnection, credentials.Username, file.Name, fileSize, startTime, endTime, id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro durante o processo: {ex.Message}");
                }
                finally
                {
                    sftp.Disconnect();
                }

                // Função atualizada para definir o diretório S3 com base no nome do arquivo
                static string GetS3KeyPrefix(string fileName)
                {
                    if (fileName.Contains("Arquivo A"))
                    {
                        return "Pasta A";
                    }
                    else if (fileName.Contains("Arquivo B"))
                    {
                        return "Pasta B";
                    }
                                        
                    else if (fileName.Contains("Arquivo C"))
                    {
                        return "Pasta C";
                    }
                    
                    else if (fileName.Contains("Arquivo D"))
                    {
                        return "Pasta D";
                    }
                    
                    else if (fileName.Contains("Arquivo E"))
                    {
                        return "Pasta E";
                    }
                    
                    else if (fileName.Contains("Arquivo F"))
                    {
                        return "Pasta F";
                    }

                    else
                    {
                        return "Não existe pasta criada para este arquivo";
                    }
                }
            }
        }

        dbConnection.Dispose();
    }

    private static async Task<Guid> LogDownloadStartAsync(IDbConnection dbConnection, string username, string fileName, DateTime startTime)
    {
        var id = Guid.NewGuid();
        Console.WriteLine($"Registrando início do download do arquivo {fileName} no banco de dados...");

        string sql = "INSERT INTO LOGS_ROBOT_LOAD (id, partner, start_time, process, load_name, load_size) VALUES (@Id, @Partner, @StartTime, @Process, @LoadName, @LoadSize)";
        var parameters = new
        {
            Id = id,
            Partner = "Nome do parceiro",
            StartTime = startTime,
            Process = username,
            LoadName = fileName,
            LoadSize = 0 // Tamanho ainda não disponível
        };

        await dbConnection.ExecuteAsync(sql, parameters);

        Console.WriteLine($"Registro de início do download do arquivo {fileName} inserido com sucesso.");

        return id;
    }

    private static async Task LogDownloadEndAsync(IDbConnection dbConnection, string username, string fileName, decimal loadSize, DateTime startTime, DateTime endTime, Guid id)
    {
        Console.WriteLine($"Registrando fim do download do arquivo {fileName} no banco de dados...");

        string updateSql = @"
        UPDATE LOGS_ROBOT_LOAD 
        SET end_time = @EndTime, load_size = @LoadSize 
        WHERE id = @Id";

        var updateParameters = new
        {
            Id = id,
            EndTime = endTime,
            LoadSize = loadSize,
        };

        try
        {
            int rowsAffected = await dbConnection.ExecuteAsync(updateSql, updateParameters);
            if (rowsAffected > 0)
            {
                Console.WriteLine($"Registro de fim do download do arquivo {fileName} atualizado com sucesso.");
            }
            else
            {
                Console.WriteLine($"Nenhum registro atualizado para o arquivo {fileName}. Verifique os critérios da cláusula WHERE.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar o registro para o arquivo {fileName}: {ex.Message}");
        }
    }
}


