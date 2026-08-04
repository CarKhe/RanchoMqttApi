using System.ComponentModel.DataAnnotations;

namespace RanchoMqttApi;

public class Users
{
    [Key]
    public int idUser { get; set; }
    public  string userName { get; set; } = string.Empty;
    public  string userMail { get; set; } = string.Empty;
    public  string passwordHash { get; set; } = string.Empty;
    public DateTime createDate { get; set; }
    public DateTime updatedLogin { get; set; }

}
