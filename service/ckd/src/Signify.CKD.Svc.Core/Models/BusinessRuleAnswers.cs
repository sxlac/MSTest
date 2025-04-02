using System;

namespace Signify.CKD.Svc.Core.Models;

public abstract class BusinessRuleAnswers
{
    public DateTime? ExpirationDate { get; set; }
    public DateTime? DateOfService { get; set; }
    public string CkdAnswer { get; set; }
    public bool IsPerformed { get; set; }
}