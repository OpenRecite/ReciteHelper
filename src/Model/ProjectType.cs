using ReciteHelper.Core.Enums;
using ReciteHelper.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ReciteHelper.Model;

/// <summary>
/// Represents a project type with associated metadata, such as name, description, icon, and template category.
/// </summary>
/// <remarks>This class implements <see cref="INotifyPropertyChanged"/>, enabling data binding scenarios where UI
/// elements automatically update in response to property changes. It is typically used to describe and categorize
/// different types of projects within an application.</remarks>
public class ProjectType : INotifyPropertyChanged
{
    public int Id
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Id));
        }
    }

    public string TypeName
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(nameof(TypeName));
        }
    }

    public string Description
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Description));
        }
    }

    public string IconPath
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IconPath));
        }
    }

    public ProjectTemplateType TemplateType
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(nameof(TemplateType));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
