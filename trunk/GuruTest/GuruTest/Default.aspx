﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="GuruTest._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:Login ID="Login1" runat="server" VisibleWhenLoggedIn="False" 
            PasswordRecoveryText="Odzyskiwanie hasełka" 
            PasswordRecoveryUrl="http://www.wp.pl">
        </asp:Login>
    
    </div>
    <asp:Button ID="Button1" runat="server" onclick="Button1_Click" Text="Button" />
    <asp:ChangePassword ID="ChangePassword1" runat="server">
    </asp:ChangePassword>
    </form>
</body>
</html>
