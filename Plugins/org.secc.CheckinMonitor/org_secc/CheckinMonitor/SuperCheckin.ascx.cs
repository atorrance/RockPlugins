﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Model;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Security;
using System.Web.UI;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Super Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Advanced tool for managing checkin." )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "Connection status for new people." )]
    [AttributeCategoryField( "Checkin Category", "The Attribute Category to display checkin attributes from", false, "Rock.Model.Person", true, "", "", 0 )]
    [TextField( "Checkin Activity", "Name of the activity to complete checkin", true )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "SMS Phone", "Phone number type to save as when SMS enabled" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Other Phone", "Phone number type to save as when SMS NOT enabled" )]
    public partial class SuperCheckin : CheckInBlock
    {

        private RockContext _rockContext;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
                return;
            }

            if ( _rockContext == null )
            {
                _rockContext = new RockContext();
            }

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

            if ( !Page.IsPostBack )
            {
                if ( CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
                {
                    ViewState.Add( "ExistingFamily", true );
                    pnlManageFamily.Visible = true;
                    ActivateFamily();
                    SaveViewState();
                }
                else
                {
                    ViewState.Add( "ExistingFamily", false );
                    pnlNewFamily.Visible = true;
                    BuildNewFamilyControls();
                    SaveViewState();
                }
            }
            else
            {
                bool existingFamily = ( bool ) ViewState["ExistingFamily"];
                if ( existingFamily )
                {
                    DisplayFamilyMemberMenu();
                    BuildGroupTypeModal();
                }

                if ( pnlEditPerson.Visible )
                {
                    var personId = ( int ) ViewState["SelectedPersonId"];
                    if ( personId != 0 )
                    {
                        EditPerson( new PersonService( _rockContext ).Get( personId ), false );
                    }
                }
            }

            if ( CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() &&
                CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .FirstOrDefault()
                .People.SelectMany( p => p.GroupTypes )
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .Where( s => s.Selected )
                .Any()
                )
            {
                btnCompleteCheckin.Visible = true;
            }
            else
            {
                btnCompleteCheckin.Visible = false;
            }

        }

        private void BuildNewFamilyControls()
        {
            ddlAdult1Suffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );
            ddlAdult2Suffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );

            rblAdult1Gender.Items.Clear();
            rblAdult1Gender.Items.Add( new ListItem( Gender.Male.ConvertToString(), Gender.Male.ConvertToInt().ToString() ) );
            rblAdult1Gender.Items.Add( new ListItem( Gender.Female.ConvertToString(), Gender.Female.ConvertToInt().ToString() ) );
            rblAdult1Gender.Items.Add( new ListItem( Gender.Unknown.ConvertToString(), Gender.Unknown.ConvertToInt().ToString() ) );

            rblAdult2Gender.Items.Clear();
            rblAdult2Gender.Items.Add( new ListItem( Gender.Male.ConvertToString(), Gender.Male.ConvertToInt().ToString() ) );
            rblAdult2Gender.Items.Add( new ListItem( Gender.Female.ConvertToString(), Gender.Female.ConvertToInt().ToString() ) );
            rblAdult2Gender.Items.Add( new ListItem( Gender.Unknown.ConvertToString(), Gender.Unknown.ConvertToInt().ToString() ) );

            pnbAdult1Phone.Text = CurrentCheckInState.CheckIn.SearchValue;

            var campusList = CampusCache.All();

            if ( campusList.Any() )
            {
                cpNewFamilyCampus.DataSource = campusList;
                cpNewFamilyCampus.DataBind();
            }
        }

        private void ActivateFamily()
        {
            List<string> errorMessages = new List<string>();
            ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errorMessages );
            if ( errorMessages.Any() )
            {
                NavigateToPreviousPage();
            }
            DisplayFamilyMemberMenu();
        }

        private void DisplayFamilyMemberMenu()
        {
            phFamilyMembers.Controls.Clear();

            foreach ( var checkinPerson in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People )
            {
                BootstrapButton btnMember = new BootstrapButton();
                btnMember.CssClass = "btn btn-default btn-block btn-lg";
                btnMember.Text = "<b>" + checkinPerson.Person.FullName + " (" + GetSelectedCount( checkinPerson ).ToString() + ")</b><br>" + checkinPerson.Person.FormatAge();
                if ( !checkinPerson.FamilyMember )
                {
                    btnMember.Text = "<i class='fa fa-exchange'></i> " + btnMember.Text;
                }
                btnMember.ID = checkinPerson.Person.Id.ToString();
                btnMember.Click += ( s, e ) =>
                {
                    ViewState["SelectedPersonId"] = checkinPerson.Person.Id;
                    SaveViewState();
                    DisplayPersonInformation();
                };
                phFamilyMembers.Controls.Add( btnMember );
            }
        }

        private int GetSelectedCount( CheckInPerson checkinPerson )
        {
            return checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .Where( s => s.Selected ).Count();
        }

        private void DisplayPersonInformation()
        {
            pnlAddPerson.Visible = false;
            pnlPersonInformation.Visible = true;
            pnlEditPerson.Visible = false;

            int selectedPersonId;
            if ( ViewState["SelectedPersonId"] != null )
            {
                selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();
                pnlPersonInformation.Visible = true;
                pnlAddPerson.Visible = false;
                ltName.Text = checkinPerson.Person.FullName;
                BuildPersonCheckinDetails();
            }

        }

        private void BuildPersonCheckinDetails()
        {
            if ( ViewState["SelectedPersonId"] != null )
            {
                var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                Person person = new PersonService( _rockContext ).Get( selectedPersonId );
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var reserved = attendanceService.Queryable().Where( a => a.CreatedDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == false && a.PersonAliasId == person.PrimaryAliasId ).ToList();
                var current = attendanceService.Queryable().Where( a => a.CreatedDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == true && a.EndDateTime == null && a.PersonAliasId == person.PrimaryAliasId ).ToList();
                var history = attendanceService.Queryable().Where( a => a.CreatedDateTime > Rock.RockDateTime.Today
                    && a.DidAttend == true && a.EndDateTime != null && a.PersonAliasId == person.PrimaryAliasId ).ToList();
                if ( reserved.Any() )
                {
                    pnlReserved.Visible = true;
                    gReserved.DataSource = reserved;
                    gReserved.DataBind();
                }
                else
                {
                    pnlReserved.Visible = false;
                }
                if ( current.Any() )
                {
                    pnlCheckedin.Visible = true;
                    gCheckedin.DataSource = current;
                    gCheckedin.DataBind();
                }
                else
                {
                    pnlCheckedin.Visible = false;
                }
                if ( history.Any() )
                {
                    pnlHistory.Visible = true;
                    gHistory.DataSource = history;
                    gHistory.DataBind();
                }
                else
                {
                    pnlHistory.Visible = false;
                }
            }
        }

        protected void CheckinReserved_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceItem = new AttendanceService( _rockContext ).Get( attendanceItemId );
            attendanceItem.DidAttend = true;
            attendanceItem.StartDateTime = Rock.RockDateTime.Now;
            _rockContext.SaveChanges();
            BuildPersonCheckinDetails();
        }

        protected void CancelReserved_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceService = new AttendanceService( _rockContext );
            var attendanceItem = attendanceService.Get( attendanceItemId );
            attendanceService.Delete( attendanceItem );
            _rockContext.SaveChanges();
            BuildPersonCheckinDetails();
        }

        protected void Checkout_Click( object sender, RowEventArgs e )
        {
            var attendanceItemId = ( int ) e.RowKeyValue;
            var attendanceItem = new AttendanceService( _rockContext ).Get( attendanceItemId );
            attendanceItem.EndDateTime = Rock.RockDateTime.Now;
            _rockContext.SaveChanges();
            BuildPersonCheckinDetails();
        }


        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

            int selectedPersonId;
            if ( ViewState["SelectedPersonId"] != null )
            {
                selectedPersonId = ( int ) ViewState["SelectedPersonId"];
                var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();
                CheckinModal();
            }
        }

        private void CheckinModal()
        {
            BuildGroupTypeModal();
            mdCheckin.Show();
        }

        private void BuildGroupTypeModal()
        {
            if ( ViewState["SelectedPersonId"] == null )
            {
                return;
            }
            var selectedPersonId = ( int ) ViewState["SelectedPersonId"];
            var checkinPerson = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).First().People.Where( p => p.Person.Id == selectedPersonId ).First();

            phCheckin.Controls.Clear();

            foreach ( var groupType in checkinPerson.GroupTypes )
            {
                //ignore group types with no non-excluded groups on non-super checkin
                if ( !cbSuperCheckin.Checked && !groupType.Groups.Where( g => !g.ExcludedByFilter ).Any() )
                {
                    continue;
                }

                PanelWidget panelWidget = new PanelWidget();
                panelWidget.Title = groupType.GroupType.Name;
                panelWidget.ID = groupType.GroupType.Guid.ToString();
                phCheckin.Controls.Add( panelWidget );
                foreach ( var group in groupType.Groups )
                {
                    if ( !cbSuperCheckin.Checked && group.ExcludedByFilter )
                    {
                        continue;
                    }
                    PanelWidget groupWidget = new PanelWidget();
                    groupWidget.Title = group.Group.Name;
                    if ( group.ExcludedByFilter )
                    {
                        groupWidget.Title = "<i class='fa fa-exclamation-triangle'></i> " + groupWidget.Title;
                    }
                    groupWidget.ID = group.Group.Guid.ToString();
                    panelWidget.Controls.Add( groupWidget );
                    foreach ( var location in group.Locations )
                    {
                        if ( !cbSuperCheckin.Checked && !location.Location.IsActive )
                        {
                            continue;
                        }

                        foreach ( var schedule in location.Schedules )
                        {
                            if ( !cbSuperCheckin.Checked && !schedule.Schedule.IsCheckInActive )
                            {
                                continue;
                            }

                            BootstrapButton btnSelect = new BootstrapButton();
                            btnSelect.Text = location.Location.Name + ": " + schedule.Schedule.Name;
                            if ( !location.Location.IsActive )
                            {
                                btnSelect.Text += " [Location Not Active]";
                            }

                            if ( !schedule.Schedule.IsCheckInActive )
                            {
                                btnSelect.Text += " [Schedule Not Active]";
                            }
                            btnSelect.ID = location.Location.Guid.ToString() + schedule.Schedule.Guid.ToString();
                            if ( schedule.Selected )
                            {
                                btnSelect.CssClass = "btn btn-success btn-block";
                                panelWidget.Expanded = true;
                                groupWidget.Expanded = true;
                            }
                            else
                            {
                                btnSelect.CssClass = "btn btn-default btn-block";
                            }
                            btnSelect.Click += ( s, e ) =>
                             {
                                 schedule.Selected = !schedule.Selected;
                                 SaveState();
                                 BuildGroupTypeModal();
                             };

                            groupWidget.Controls.Add( btnSelect );
                        }
                    }
                }
            }
        }

        protected void btnEditPerson_Click( object sender, EventArgs e )
        {
            var personId = ( int ) ViewState["SelectedPersonId"];
            if ( personId != 0 )
            {
                EditPerson( new PersonService( _rockContext ).Get( personId ) );
            }
        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void cbSuperCheckin_CheckedChanged( object sender, EventArgs e )
        {
            BuildGroupTypeModal();
        }

        protected void mdCheckin_SaveClick( object sender, EventArgs e )
        {
            mdCheckin.Hide();
            BuildPersonCheckinDetails();
        }

        protected void btnNewMember_Click( object sender, EventArgs e )
        {
            ShowAddNewPerson();
        }

        private void ShowAddNewPerson()
        {
            pnlEditPerson.Visible = false;
            pnlPersonInformation.Visible = false;
            pnlAddPerson.Visible = true;

            ddlNewPersonSuffix.BindToDefinedType( DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() ), true );

            rblNewPersonGender.Items.Clear();
            rblNewPersonGender.Items.Add( new ListItem( Gender.Male.ConvertToString(), Gender.Male.ConvertToInt().ToString() ) );
            rblNewPersonGender.Items.Add( new ListItem( Gender.Female.ConvertToString(), Gender.Female.ConvertToInt().ToString() ) );
            rblNewPersonGender.Items.Add( new ListItem( Gender.Unknown.ConvertToString(), Gender.Unknown.ConvertToInt().ToString() ) );
        }

        protected void btnSaveAddPerson_Click( object sender, EventArgs e )
        {
            Person person = new Person();
            person.FirstName = tbNewPersonFirstName.Text;
            person.LastName = tbNewPersonLastName.Text;
            person.SetBirthDate( dpNewPersonBirthDate.Text.AsDateTime() );
            person.ConnectionStatusValueId = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;
            if ( !string.IsNullOrWhiteSpace( rblAdult1Gender.SelectedValue ) )
            {
                person.Gender = rblNewPersonGender.SelectedValueAsEnum<Gender>();
            }
            else
            {
                person.Gender = Gender.Unknown;
            }

            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var childRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();
            var age = person.Age;
            int familyRoleId = age.HasValue && age < 18 ? childRoleId : adultRoleId;

            var family = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().Group;
            if ( cbRelationship.Checked )
            {
                PersonService.AddPersonToFamily( person, true, family.Id, familyRoleId, _rockContext );
            }
            else
            {
                //save the person with new family
                var newFamily = PersonService.SaveNewPerson( person, _rockContext );

                //create connection
                var memberService = new GroupMemberService( _rockContext );
                var adultFamilyMembers = memberService.Queryable()
                    .Where( m =>
                        m.GroupId == family.Id
                        && m.GroupRoleId == adultRoleId
                        )
                        .Select( m => m.Person )
                        .ToList();

                foreach ( var member in adultFamilyMembers )
                {
                    Person.CreateCheckinRelationship( member.Id, person.Id, _rockContext );
                }
            }
            ViewState["SelectedPersonId"] = person.Id;
            ActivateFamily();
            EditPerson( person );
        }

        private void EditPerson( Person person, bool setValue = true )
        {
            pnlAddPerson.Visible = false;
            pnlPersonInformation.Visible = false;
            pnlEditPerson.Visible = true;

            ltEditName.Text = person.FullName;

            var AttributeList = new List<int>();

            string categoryGuid = GetAttributeValue( "CheckinCategory" );
            Guid guid = Guid.Empty;
            if ( Guid.TryParse( categoryGuid, out guid ) )
            {
                var category = CategoryCache.Read( guid );
                if ( category != null )
                {
                    AttributeList = new AttributeService( _rockContext ).GetByCategoryId( category.Id )
                        .OrderBy( a => a.Order ).ThenBy( a => a.Name ).Select( a => a.Id ).ToList();
                }
            }
            person.LoadAttributes();

            foreach ( int attributeId in AttributeList )
            {
                var attribute = AttributeCache.Read( attributeId );
                string attributeValue = person.GetAttributeValue( attribute.Key );
                attribute.AddControl( fsAttributes.Controls, attributeValue, "", setValue, true );
            }
        }

        protected void btnSaveAttributes_Click( object sender, EventArgs e )
        {
            var personId = ( int ) ViewState["SelectedPersonId"];

            var person = new PersonService( _rockContext ).Get( personId );

            int personEntityTypeId = EntityTypeCache.Read( typeof( Person ) ).Id;

            var AttributeList = new List<int>();

            string categoryGuid = GetAttributeValue( "CheckinCategory" );
            Guid guid = Guid.Empty;
            if ( Guid.TryParse( categoryGuid, out guid ) )
            {
                var category = CategoryCache.Read( guid );
                if ( category != null )
                {
                    AttributeList = new AttributeService( _rockContext ).GetByCategoryId( category.Id )
                        .OrderBy( a => a.Order ).ThenBy( a => a.Name ).Select( a => a.Id ).ToList();
                }
            }
            person.LoadAttributes();

            var changes = new List<string>();
            foreach ( int attributeId in AttributeList )
            {
                var attribute = AttributeCache.Read( attributeId );

                if ( person != null &&
                    attribute.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson ) )
                {
                    System.Web.UI.Control attributeControl = fsAttributes.FindControl( string.Format( "attribute_field_{0}", attribute.Id ) );
                    if ( attributeControl != null )
                    {
                        string originalValue = person.GetAttributeValue( attribute.Key );
                        string newValue = attribute.FieldType.Field.GetEditValue( attributeControl, attribute.QualifierValues );
                        Rock.Attribute.Helper.SaveAttributeValue( person, attribute, newValue, _rockContext );

                        // Check for changes to write to history
                        if ( ( originalValue ?? string.Empty ).Trim() != ( newValue ?? string.Empty ).Trim() )
                        {
                            string formattedOriginalValue = string.Empty;
                            if ( !string.IsNullOrWhiteSpace( originalValue ) )
                            {
                                formattedOriginalValue = attribute.FieldType.Field.FormatValue( null, originalValue, attribute.QualifierValues, false );
                            }

                            string formattedNewValue = string.Empty;
                            if ( !string.IsNullOrWhiteSpace( newValue ) )
                            {
                                formattedNewValue = attribute.FieldType.Field.FormatValue( null, newValue, attribute.QualifierValues, false );
                            }

                            History.EvaluateChange( changes, attribute.Name, formattedOriginalValue, formattedNewValue );
                        }
                    }
                }
            }
            if ( changes.Any() )
            {
                HistoryService.SaveChanges( _rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                    person.Id, changes );
            }
            pnlPersonInformation.Visible = true;
            pnlEditPerson.Visible = false;
        }

        protected void btnCancelAttributes_Click( object sender, EventArgs e )
        {
            pnlPersonInformation.Visible = true;
            pnlEditPerson.Visible = false;
        }

        protected void btnCompleteCheckin_Click( object sender, EventArgs e )
        {
            foreach ( var person in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().People )
            {
                if ( person.GroupTypes.SelectMany( gt => gt.Groups ).SelectMany( g => g.Locations ).SelectMany( l => l.Schedules ).Where( s => s.Selected ).Any() )
                {
                    person.Selected = true;
                    foreach ( var groupType in person.GroupTypes )
                    {
                        groupType.Selected = true;
                        if ( groupType.Groups.SelectMany( g => g.Locations ).SelectMany( l => l.Schedules ).Where( s => s.Selected ).Any() )
                        {
                            foreach ( var group in groupType.Groups )
                            {
                                if ( group.Locations.SelectMany( l => l.Schedules ).Where( s => s.Selected ).Any() )
                                {
                                    group.Selected = true;
                                    foreach ( var location in group.Locations )
                                    {
                                        if ( location.Schedules.Where( s => s.Selected ).Any() )
                                        {
                                            location.Selected = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    person.Selected = false;
                }
            }
            SaveState();
            var activity = GetAttributeValue( "CheckinActivity" );
            List<string> errorMessages;
            ProcessActivity( activity, out errorMessages );
            ProcessLabels();
        }
        private void ProcessLabels()
        {
            var printQueue = new Dictionary<string, StringBuilder>();
            foreach ( var selectedFamily in CurrentCheckInState.CheckIn.Families.Where( p => p.Selected ) )
            {
                List<CheckInLabel> labels = new List<CheckInLabel>();
                List<CheckInPerson> selectedPeople = selectedFamily.People.Where( p => p.Selected ).ToList();
                List<FamilyLabel> familyLabels = new List<FamilyLabel>();

                foreach ( CheckInPerson selectedPerson in selectedPeople )
                {
                    foreach ( var groupType in selectedPerson.GroupTypes.Where( gt => gt.Selected ) )
                    {

                        foreach ( var label in groupType.Labels )
                        {
                            var file = new BinaryFileService( _rockContext ).Get( label.FileGuid );
                            file.LoadAttributes( _rockContext );
                            string isFamilyLabel = file.GetAttributeValue( "IsFamilyLabel" );
                            if ( isFamilyLabel != "True" )
                            {
                                labels.Add( label );
                            }
                            else
                            {
                                List<string> mergeCodes = file.GetAttributeValue( "MergeCodes" ).TrimEnd( '|' ).Split( '|' ).ToList();
                                FamilyLabel familyLabel = familyLabels.FirstOrDefault( fl => fl.FileGuid == label.FileGuid &&
                                                                                 fl.MergeFields.Count < mergeCodes.Count );
                                if ( familyLabel == null )
                                {
                                    familyLabel = new FamilyLabel();
                                    familyLabel.FileGuid = label.FileGuid;
                                    familyLabel.LabelObj = label;
                                    foreach ( var mergeCode in mergeCodes )
                                    {
                                        familyLabel.MergeKeys.Add( mergeCode.Split( '^' )[0] );
                                    }
                                    familyLabels.Add( familyLabel );
                                }
                                familyLabel.MergeFields.Add( ( selectedPerson.Person.Age.ToString() ?? "#" ) + "yr-" + selectedPerson.SecurityCode );
                            }
                        }
                    }
                }

                //Format all FamilyLabels and add to list of labels to print.
                foreach ( FamilyLabel familyLabel in familyLabels )
                {
                    //create padding to clear unused merge fields
                    List<string> padding = Enumerable.Repeat( " ", familyLabel.MergeKeys.Count ).ToList();
                    familyLabel.MergeFields.AddRange( padding );
                    for ( int i = 0; i < familyLabel.MergeKeys.Count; i++ )
                    {
                        familyLabel.LabelObj.MergeFields[familyLabel.MergeKeys[i]] = familyLabel.MergeFields[i];
                    }
                    labels.Add( familyLabel.LabelObj );
                }

                // Print client labels
                if ( labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Client ) )
                {
                    var clientLabels = labels.Where( l => l.PrintFrom == PrintFrom.Client ).ToList();
                    var urlRoot = string.Format( "{0}://{1}", Request.Url.Scheme, Request.Url.Authority );
                    clientLabels.ForEach( l => l.LabelFile = urlRoot + l.LabelFile );
                    AddLabelScript( clientLabels.ToJson() );
                }

                // Print server labels
                if ( labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Server ) )
                {
                    string delayCut = @"^XB";
                    string endingTag = @"^XZ";
                    var printerIp = string.Empty;
                    var labelContent = new StringBuilder();

                    // make sure labels have a valid ip
                    var lastLabel = labels.Last();
                    foreach ( var label in labels.Where( l => l.PrintFrom == PrintFrom.Server && !string.IsNullOrEmpty( l.PrinterAddress ) ) )
                    {
                        var labelCache = KioskLabel.Read( label.FileGuid );
                        if ( labelCache != null )
                        {
                            if ( printerIp != label.PrinterAddress )
                            {
                                printQueue.AddOrReplace( label.PrinterAddress, labelContent );
                                printerIp = label.PrinterAddress;
                                labelContent = new StringBuilder();
                            }

                            var printContent = labelCache.FileContent;
                            foreach ( var mergeField in label.MergeFields )
                            {
                                if ( !string.IsNullOrWhiteSpace( mergeField.Value ) )
                                {
                                    printContent = Regex.Replace( printContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", mergeField.Key ), ZebraFormatString( mergeField.Value ) );
                                }
                                else
                                {
                                    printContent = Regex.Replace( printContent, string.Format( @"\^FO.*\^FS\s*(?=\^FT.*\^FD{0}\^FS)", mergeField.Key ), string.Empty );
                                    printContent = Regex.Replace( printContent, string.Format( @"\^FD{0}\^FS", mergeField.Key ), "^FD^FS" );
                                }
                            }

                            // send a Delay Cut command at the end to prevent cutting intermediary labels
                            if ( label != lastLabel )
                            {
                                printContent = Regex.Replace( printContent.Trim(), @"\" + endingTag + @"$", delayCut + endingTag );
                            }

                            labelContent.Append( printContent );
                        }
                    }

                    printQueue.AddOrReplace( printerIp, labelContent );
                }

                if ( printQueue.Any() )
                {
                    PrintLabels( printQueue );
                    printQueue.Clear();
                }
            }
            ClearCheckin();
        }

        /// <summary>
        /// Prints the labels.
        /// </summary>
        /// <param name="families">The families.</param>
        private void PrintLabels( Dictionary<string, StringBuilder> printerContent )
        {
            foreach ( var printerIp in printerContent.Keys.Where( k => !string.IsNullOrEmpty( k ) ) )
            {
                StringBuilder labelContent;
                if ( printerContent.TryGetValue( printerIp, out labelContent ) )
                {
                    var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                    var printerIpEndPoint = new IPEndPoint( IPAddress.Parse( printerIp ), 9100 );
                    var result = socket.BeginConnect( printerIpEndPoint, null, null );
                    bool success = result.AsyncWaitHandle.WaitOne( 5000, true );

                    if ( socket.Connected )
                    {
                        var ns = new NetworkStream( socket );
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes( labelContent.ToString() );
                        ns.Write( toSend, 0, toSend.Length );
                    }
                    else
                    {
                        //phPrinterStatus.Controls.Add(new LiteralControl(string.Format("Can't connect to printer: {0}", printerIp)));
                    }

                    if ( socket != null && socket.Connected )
                    {
                        socket.Shutdown( SocketShutdown.Both );
                        socket.Close();
                    }
                }
            }

        }

        private void ClearCheckin()
        {
            foreach ( var person in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().People )
            {
                person.Selected = false;
                foreach ( var groupType in person.GroupTypes )
                {
                    groupType.Selected = false;
                    foreach ( var group in groupType.Groups )
                    {
                        group.Selected = false;
                        foreach ( var location in group.Locations )
                        {
                            location.Selected = false;
                            foreach ( var schedule in location.Schedules )
                            {
                                schedule.Selected = false;
                            }
                        }
                    }
                }
            }
            SaveState();
            BuildPersonCheckinDetails();
        }

        /// <summary>
        /// Adds the label script.
        /// </summary>
        /// <param name="jsonObject">The json object.</param>
        private void AddLabelScript( string jsonObject )
        {
            string script = string.Format( @"
            var labelData = {0};
		    function printLabels() {{
		        ZebraPrintPlugin.printTags(
            	    JSON.stringify(labelData),
            	    function(result) {{
			        }},
			        function(error) {{
				        // error is an array where:
				        // error[0] is the error message
				        // error[1] determines if a re-print is possible (in the case where the JSON is good, but the printer was not connected)
			            console.log('An error occurred: ' + error[0]);
                        navigator.notification.alert(
                            'An error occurred while printing the labels.' + error[0],  // message
                            alertDismissed,         // callback
                            'Error',            // title
                            'Ok'                  // buttonName
                        );
			        }}
                );
	        }}
try{{
            printLabels();
}} catch(e){{}}
            ", jsonObject, btnBack.UniqueID );
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
        }

        private static string ZebraFormatString( string input, bool isJson = false )
        {
            if ( isJson )
            {
                return input.Replace( "é", @"\\82" );  // fix acute e
            }
            else
            {
                return input.Replace( "é", @"\82" );  // fix acute e
            }
        }

        protected void btnNewFamily_Click( object sender, EventArgs e )
        {
            var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            var adultRoleId = familyGroupType.Roles
                .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                .Select( r => r.Id )
                .FirstOrDefault();

            //Adult1 Basic Info
            Person adult1 = new Person();
            adult1.FirstName = tbAdult1FirstName.Text;
            adult1.LastName = tbAdult1LastName.Text;
            adult1.SuffixValueId = ddlAdult1Suffix.SelectedValueAsId();
            adult1.ConnectionStatusValueId = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;



            if ( !string.IsNullOrWhiteSpace( rblAdult1Gender.SelectedValue ) )
            {
                adult1.Gender = rblAdult1Gender.SelectedValueAsEnum<Gender>();
            }
            else
            {
                adult1.Gender = Gender.Unknown;
            }

            var newFamily = PersonService.SaveNewPerson( adult1, _rockContext );
            newFamily.Members.Where( m => m.Person == adult1 ).FirstOrDefault().GroupRoleId = adultRoleId;

            int homeLocationTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;
            var familyLocation = new Location();
            familyLocation.Street1 = acNewFamilyAddress.Street1;
            familyLocation.Street2 = acNewFamilyAddress.Street2;
            familyLocation.City = acNewFamilyAddress.City;
            familyLocation.State = acNewFamilyAddress.State;
            familyLocation.PostalCode = acNewFamilyAddress.PostalCode;
            newFamily.GroupLocations.Add( new GroupLocation() { Location = familyLocation, GroupLocationTypeValueId = homeLocationTypeId } );
            newFamily.CampusId = cpNewFamilyCampus.SelectedCampusId;

            _rockContext.SaveChanges();

            if ( cbAdult1SMS.Checked )
            {
                var smsPhone = DefinedValueCache.Read( GetAttributeValue( "SMSPhone" ).AsGuid() ).Id;
                adult1.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbAdult1Phone.Text, true, false, _rockContext );
            }
            else
            {
                var otherPhone = DefinedValueCache.Read( GetAttributeValue( "OtherPhone" ).AsGuid() ).Id;
                adult1.UpdatePhoneNumber( otherPhone, PhoneNumber.DefaultCountryCode(), pnbAdult1Phone.Text, false, false, _rockContext );
            }

            if ( !string.IsNullOrWhiteSpace( tbAdult2FirstName.Text ) && !string.IsNullOrWhiteSpace( tbAdult2LastName.Text ) )
            {
                //Adult2 Basic Info
                Person adult2 = new Person();
                adult2.FirstName = tbAdult2FirstName.Text;
                adult2.LastName = tbAdult2LastName.Text;
                adult2.SuffixValueId = ddlAdult2Suffix.SelectedValueAsId();
                adult2.ConnectionStatusValueId = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() ).Id;

                if ( !string.IsNullOrWhiteSpace( rblAdult2Gender.SelectedValue ) )
                {
                    adult2.Gender = rblAdult1Gender.SelectedValueAsEnum<Gender>();
                }
                else
                {
                    adult2.Gender = Gender.Unknown;
                }

                PersonService.AddPersonToFamily( adult2, true, newFamily.Id, adultRoleId, _rockContext );

                if ( cbAdult2SMS.Checked )
                {
                    var smsPhone = DefinedValueCache.Read( GetAttributeValue( "SMSPhone" ).AsGuid() ).Id;
                    adult2.UpdatePhoneNumber( smsPhone, PhoneNumber.DefaultCountryCode(), pnbAdult2Phone.Text, true, false, _rockContext );
                }
                else
                {
                    var otherPhone = DefinedValueCache.Read( GetAttributeValue( "OtherPhone" ).AsGuid() ).Id;
                    adult2.UpdatePhoneNumber( otherPhone, PhoneNumber.DefaultCountryCode(), pnbAdult2Phone.Text, false, false, _rockContext );
                }
            }

            CurrentCheckInState.CheckIn.Families.Add( new CheckInFamily() { Group = newFamily, Selected = true } );
            SaveState();
            ViewState.Add( "ExistingFamily", true );
            pnlManageFamily.Visible = true;
            pnlNewFamily.Visible = false;
            SaveViewState();
            ActivateFamily();

        }
    }
    public class FamilyLabel
    {
        public Guid FileGuid { get; set; }

        public CheckInLabel LabelObj { get; set; }

        private List<string> _mergeFields = new List<string>();
        public List<string> MergeFields
        {
            get
            {
                return _mergeFields;
            }
            set
            {
                _mergeFields = value;
            }
        }
        private List<string> _mergeKeys = new List<string>();

        public List<string> MergeKeys
        {
            get
            {
                return _mergeKeys;
            }
            set
            {
                _mergeKeys = value;
            }
        }
    }
}