<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login2.aspx.cs" Inherits="GuruTest.login" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:Login ID="Login1" runat="server" VisibleWhenLoggedIn="False" 
            PasswordRecoveryText="Odzyskiwanie hasełka" 
            PasswordRecoveryUrl="http://www.wp.pl">
        </asp:Login>
    
    </div>
    <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
    </form>
</body>
</html>
