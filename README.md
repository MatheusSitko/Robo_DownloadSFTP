# Robô de Download e Upload SFTP para S3

Este projeto foi desenvolvido para automatizar o download de arquivos de múltiplos servidores SFTP e realizar o upload para o Amazon S3.


## Dependências

- [Renci.SshNet](https://www.nuget.org/packages/Renci.SshNet)
- [Dapper](https://www.nuget.org/packages/Dapper)
- [MySql.Data](https://www.nuget.org/packages/MySql.Data)
- [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
- [Amazon.S3](https://www.nuget.org/packages/Amazon.S3)
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [Amazon.S3.Transfer](https://www.nuget.org/packages/AWSSDK.S3)



## Funcionamento.

O robô funciona da seguinte maneira:


## Configuração dos SFTP: 
Preencha a lista de credenciais para os SFTP que serão acessados.
---
private static List<SftpCredentials> sftpCredentialsList = new List<SftpCredentials>
{
    new SftpCredentials { Host = "", Username = "", Password = "" },
    // Adicione mais SFTP conforme necessário
};
---




## Classe de Credenciais: 
A classe SftpCredentials armazena as informações necessárias para autenticação.

---
private class SftpCredentials
{
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int Port { get; set; } = //ID port;
}
---




## Conexão e Download: 
O robô itera sobre a lista de SFTP, conecta-se a cada um e baixa os arquivos que foram modificados no dia atual.

---
foreach (var credentials in sftpCredentialsList)
{
    using (var sftp = new SftpClient(credentials.Host, credentials.Port, credentials.Username, credentials.Password))
    {
        sftp.Connect();
        var files = sftp.ListDirectory("/Remote Directory");
        // Filtra arquivos do dia
    }
}
---




## Upload para S3: 
Os arquivos baixados são armazenados em um MemoryStream e enviados diretamente para o Amazon S3.

---
var serviceCollection = new ServiceCollection();
// Configuração do AWS S3
await fileTransferUtility.UploadAsync(memoryStream, bucketName, fullPath);
---




## Registro de Logs: 
O robô registra o início e o fim de cada download em um banco de dados, facilitando o monitoramento do processo.

---
await LogDownloadStartAsync(dbConnection, credentials.Username, file.Name, startTime);
await LogDownloadEndAsync(dbConnection, credentials.Username, file.Name, fileSize, startTime, endTime, id);
---




## Tratamento de Erros: 
Caso ocorra um erro durante o processo, ele é capturado e uma mensagem de erro é exibida.

---
catch (Exception ex)
{
    Console.WriteLine($"Erro durante o processo: {ex.Message}");
}
---




## Desconexão: 
Após processar todos os arquivos, o robô se desconecta do SFTP e passa para o próximo.
finally

---
{
    sftp.Disconnect();
}
---
