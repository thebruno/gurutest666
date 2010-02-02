<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="adduser.aspx.cs" Inherits="GuruTest.adduser" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
    </div>
    <asp:CreateUserWizard ID="cuWizard" runat="server" 
        CreateUserButtonText="Weiter" LoginCreatedUser="False" 
        oncreateduser="CreateUserWizard1_CreatedUser" 
        onfinishbuttonclick="cuWizard_FinishButtonClick">
        <WizardSteps>
            <asp:CreateUserWizardStep runat="server" />
            <asp:WizardStep runat="server">
                <asp:TextBox ID="txtCity" runat="server"></asp:TextBox>
            </asp:WizardStep>
            <asp:CompleteWizardStep runat="server" />
        </WizardSteps>
    </asp:CreateUserWizard>
    </form>
</body>
</html>
