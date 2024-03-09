﻿using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CriminalRecords;

namespace Content.Client.CartridgeLoader.Cartridges;

[GenerateTypedNameReferences]
public sealed partial class CriminalRecordsCartridgeUiFragment : BoxContainer
{

    //public event Action<string>? OnNoteAdded;
    //public event Action<string>? OnNoteRemoved;

    public CriminalRecordsCartridgeUiFragment()
    {
        RobustXamlLoader.Load(this);
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        UpdateState(new CriminalRecordsCartridgeUiState(new List<(string,CriminalRecord)>(), new List<(string,CriminalRecord)>()));
    }

    
    public void UpdateState(CriminalRecordsCartridgeUiState state)
    {
      foreach(var (name,record) in state.Wanted){
        AddCriminal(name, record);
      }

      foreach(var (name,record) in state.Detained){
        AddDetained(name,record);
      }
    }

    

    private void AddDetained(string name, CriminalRecord record)
    {
      var row = new BoxContainer();
      row.HorizontalExpand = true;
      row.Orientation = LayoutOrientation.Horizontal;
      row.Margin = new Thickness(4);
      
      var criminalName = new Label();
      criminalName.Text = name;
      criminalName.HorizontalExpand = true;
      criminalName.ClipText = true;
      
      row.AddChild(criminalName);
      Detained.AddChild(row);
    }

    private void AddCriminal(string name, CriminalRecord record)
    {
      var row = new BoxContainer();
      row.HorizontalExpand = true;
      row.Orientation = LayoutOrientation.Horizontal;
      row.Margin = new Thickness(4);
      
      var criminalName = new Label();
      criminalName.Text = name;
      criminalName.HorizontalExpand = true;
      criminalName.ClipText = true;
      
      row.AddChild(criminalName);
      Wanted.AddChild(row);
    }
    
    /*
    private void AddNote(string note)
    {
        var row = new BoxContainer();
        row.HorizontalExpand = true;
        row.Orientation = LayoutOrientation.Horizontal;
        row.Margin = new Thickness(4);

        var label = new Label();
        label.Text = note;
        label.HorizontalExpand = true;
        label.ClipText = true;

        var removeButton = new TextureButton();
        removeButton.AddStyleClass("windowCloseButton");
        removeButton.OnPressed += _ => OnNoteRemoved?.Invoke(label.Text);

        row.AddChild(label);
        row.AddChild(removeButton);

        MessageContainer.AddChild(row);
    }
    */
}
