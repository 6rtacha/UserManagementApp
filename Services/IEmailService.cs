namespace UserManagementApp.Services;                                                                                   
                                                                                                                            
    public interface IEmailService                                                                                          
    {                                                                                                                       
        Task<bool> SendVerificationEmailAsync(string toEmail, string userName, string verificationLink);                    
    }                                                                                                                       
                                                                                                                            
  