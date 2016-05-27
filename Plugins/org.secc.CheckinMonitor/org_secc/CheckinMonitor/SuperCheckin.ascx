﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SuperCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.SuperCheckin" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maWarning" runat="server" />

        <Rock:ModalDialog ID="mdCheckin" runat="server" Title="Check-In" OnSaveClick="mdCheckin_SaveClick" CancelLinkVisible="false" SaveButtonText="Save Check-In State">
            <Content>
                <Rock:Toggle runat="server" ID="cbSuperCheckin" OnCssClass="btn-success"
                    OffCssClass="btn-danger" OnCheckedChanged="cbSuperCheckin_CheckedChanged"
                    Checked="false" Label="Super Check-In" />
                <asp:PlaceHolder ID="phCheckin" runat="server" />
            </Content>
        </Rock:ModalDialog>

        <!-- New Family -->
        <asp:Panel ID="pnlNewFamily" Visible="false" runat="server">
            <h1>New Family</h1>
            <h2>Adult 1</h2>
            <div class="row">
                <div class="col-xs-12 col-sm-5">
                    <Rock:RockTextBox ID="tbAdult1FirstName" runat="server" Label="First Name" />
                </div>
                <div class="col-xs-12 col-sm-5">
                    <Rock:RockTextBox ID="tbAdult1LastName" runat="server" Label="Last Name" />
                </div>
                <div class="col-xs-12 col-sm-2">
                    <Rock:RockDropDownList ID="ddlAdult1Suffix" runat="server" Label="Suffix" CssClass="input-width-md" />
                </div>
                <div class="col-xs-12 col-sm-4">
                    <Rock:RockRadioButtonList ID="rblAdult1Gender" runat="server" Label="Gender" RepeatDirection="Horizontal" />
                </div>
                <div class="col-xs-8 col-sm-6">
                    <Rock:PhoneNumberBox ID="pnbAdult1Phone" runat="server" Label="Phone Number"></Rock:PhoneNumberBox>
                </div>
                <div class="col-xs-4 col-sm-2">
                    <Rock:Toggle runat="server" ID="cbAdult1SMS" Label="Is Cell Phone?" OnCssClass="btn-success"
                        OffCssClass="btn-danger" OnText="Yes" OffText="No" />
                </div>
            </div>
            <h2>Adult 2</h2>
            <div class="row">
                <div class="col-xs-12 col-sm-5">
                    <Rock:RockTextBox ID="tbAdult2FirstName" runat="server" Label="First Name" />
                </div>
                <div class="col-xs-12 col-sm-5">
                    <Rock:RockTextBox ID="tbAdult2LastName" runat="server" Label="Last Name" />
                </div>
                <div class="col-xs-12 col-sm-2">
                    <Rock:RockDropDownList ID="ddlAdult2Suffix" runat="server" Label="Suffix" CssClass="input-width-md" />
                </div>
                <div class="col-xs-12 col-sm-4">
                    <Rock:RockRadioButtonList ID="rblAdult2Gender" runat="server" Label="Gender" RepeatDirection="Horizontal" />
                </div>
                <div class="col-xs-8 col-sm-6">
                    <Rock:PhoneNumberBox ID="pnbAdult2Phone" runat="server" Label="Phone Number"></Rock:PhoneNumberBox>
                </div>
                <div class="col-xs-4 col-sm-2">
                    <Rock:Toggle runat="server" ID="cbAdult2SMS" Label="Is Cell Phone?" OnCssClass="btn-success"
                        OffCssClass="btn-danger" OnText="Yes" OffText="No" />
                </div>
            </div>
            <h2>Family Info</h2>
            <Rock:CampusPicker runat="server" ID="cpNewFamilyCampus"></Rock:CampusPicker>
            <Rock:AddressControl runat="server" ID="acNewFamilyAddress" />
            <Rock:BootstrapButton runat="server" ID="btnNewFamily" Text="Create New Family"
                 CssClass="btn btn-primary" OnClick="btnNewFamily_Click"></Rock:BootstrapButton>
        </asp:Panel>

        <!-- Manage Family -->
        <asp:Panel ID="pnlManageFamily" Visible="false" runat="server" CssClass="container">
            <div class="row">
                <div class="col-md-3" style="background-color: black; color: white; padding: 5px;">
                    <Rock:BootstrapButton runat="server" ID="btnCompleteCheckin" CssClass="btn btn-success btn-lg btn-block" OnClick="btnCompleteCheckin_Click"
                        Text="Complete Checkin<br>& Print Tags" Visible="false"></Rock:BootstrapButton>
                    <h2>Members:</h2>
                    <asp:PlaceHolder runat="server" ID="phFamilyMembers" />
                    <Rock:BootstrapButton runat="server" ID="btnNewMember" Text="Add Person" OnClick="btnNewMember_Click"
                        CssClass="btn btn-primary btn-block btn-lg"></Rock:BootstrapButton>
                    <Rock:BootstrapButton runat="server" ID="btnBack" CssClass="btn btn-danger btn-block" Text="<i class='fa fa-arrow-left' aria-hidden='true'></i>" OnClick="btnBack_Click" />
                </div>
                <div class="col-md-9">
                    <!-- Personal Info Pannel-->
                    <asp:Panel runat="server" ID="pnlPersonInformation" Visible="false">
                        <div class="row">
                            <div class="col-md-8">
                                <h1>
                                    <asp:Literal Text="" ID="ltName" runat="server" />
                                </h1>

                            </div>
                            <div class="col-md-4">
                                <Rock:BootstrapButton ID="btnCheckin" CssClass="btn btn-primary btn-lg"
                                    runat="server" Text="<i class='fa fa-check' aria-hidden='true'></i>" OnClick="btnCheckin_Click" />
                                <Rock:BootstrapButton ID="btnEditPerson" CssClass="btn btn-primary btn-lg"
                                    runat="server" Text="<i class='fa fa-pencil' aria-hidden='true'></i>" OnClick="btnEditPerson_Click" />
                            </div>
                        </div>
                        <asp:Panel runat="server" ID="pnlReserved" Visible="false">
                            <h3>Reserved:</h3>
                            <Rock:Grid runat="server" DataKeyNames="Id" ID="gReserved" ShowHeader="false" ShowFooter="false" ShowActionRow="false" DisplayType="Light">
                                <Columns>
                                    <Rock:RockBoundField DataField="Group.Name"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="Location.Name"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="Schedule.Name"></Rock:RockBoundField>
                                    <Rock:LinkButtonField OnClick="CheckinReserved_Click" Text="Checkin" CssClass="btn btn-sm btn-success"></Rock:LinkButtonField>
                                    <Rock:LinkButtonField OnClick="CancelReserved_Click" Text="Cancel" CssClass="btn btn-sm btn-danger"></Rock:LinkButtonField>
                                </Columns>
                            </Rock:Grid>
                        </asp:Panel>
                        <asp:Panel runat="server" ID="pnlCheckedin" Visible="false">
                            <h3>Checked-In:</h3>
                            <Rock:Grid runat="server" DataKeyNames="Id" ID="gCheckedin" ShowFooter="false" ShowActionRow="false" DisplayType="Light">
                                <Columns>
                                    <Rock:RockBoundField DataField="Group.Name" HeaderText="Class"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="Location.Name" HeaderText="Location"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="Schedule.Name" HeaderText="Schedule"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="StartDateTime" HeaderText="Time Arrived"></Rock:RockBoundField>
                                    <Rock:LinkButtonField OnClick="Checkout_Click" Text="Checkout" CssClass="btn btn-sm btn-danger"></Rock:LinkButtonField>
                                </Columns>
                            </Rock:Grid>
                        </asp:Panel>
                        <asp:Panel runat="server" ID="pnlHistory" Visible="false">
                            <h3>History:</h3>
                            <Rock:Grid runat="server" DataKeyNames="Id" ID="gHistory" ShowFooter="false" ShowActionRow="false" DisplayType="Light">
                                <Columns>
                                    <Rock:RockBoundField DataField="Group.Name" HeaderText="Class"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="Location.Name" HeaderText="Location"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="Schedule.Name" HeaderText="Schedule"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="StartDateTime" HeaderText="Time Arrived"></Rock:RockBoundField>
                                    <Rock:RockBoundField DataField="EndDateTime" HeaderText="Time Departed"></Rock:RockBoundField>
                                </Columns>
                            </Rock:Grid>
                        </asp:Panel>
                    </asp:Panel>

                    <asp:Panel ID="pnlAddPerson" runat="server" Visible="false">
                        <h1>Add New Person</h1>
                        <Rock:Toggle runat="server" ID="cbRelationship" Checked="true" OnText="Immediate Family Member" OffText="Other Relationship"
                            Label="Relationship" Help="Is the child part of the family (immediate), or a relative or friend (other)."
                            OnCssClass="btn-success" OffCssClass="btn-success" />
                        <div class="row">
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockTextBox ID="tbNewPersonFirstName" runat="server" Label="First Name" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockTextBox ID="tbNewPersonLastName" runat="server" Label="Last Name" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockDropDownList ID="ddlNewPersonSuffix" runat="server" Label="Suffix" CssClass="input-width-md" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockRadioButtonList ID="rblNewPersonGender" runat="server" Label="Gender" RepeatDirection="Horizontal" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:DatePicker ID="dpNewPersonBirthDate" runat="server" Label="Birthdate" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:GradePicker ID="ddlGradePicker" runat="server" Label="Grade" UseAbbreviation="true" UseGradeOffsetAsValue="true" />
                            </div>
                        </div>
                        <Rock:BootstrapButton runat="server" ID="btnSaveAddPerson" Text="Next" CssClass="btn btn-default"
                            OnClick="btnSaveAddPerson_Click"></Rock:BootstrapButton>
                    </asp:Panel>
                    <asp:Panel runat="server" ID="pnlEditPerson" Visible="false">
                        <div class="col-xs-12">
                            <h1>
                                <asp:Literal Text="" ID="ltEditName" runat="server" />
                            </h1>
                            <fieldset id="fsAttributes" runat="server" class="attribute-values"></fieldset>
                            <Rock:BootstrapButton runat="server" ID="btnSaveAttributes" CssClass="btn btn-primary"
                                OnClick="btnSaveAttributes_Click" Text="Save"></Rock:BootstrapButton>
                            <Rock:BootstrapButton runat="server" Text="Cancel" OnClick="btnCancelAttributes_Click"
                                ID="btnCancelAttributes" CssClass="btn btn-danger">

                            </Rock:BootstrapButton>
                        </div>
                    </asp:Panel>

                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>