using System;

namespace formatrack.Models;

public class Notification
{
    public int IdNotification { get; set; }
    public int IdUtilisateur { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Lue { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.Now;
}