using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace ProjectDepender
{
  public class ReferenceItem : INotifyPropertyChanged
  {
    // Implementation of INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged ;

    // Private members
    private bool             _IsProjectDependency ;
    private bool             _IsReference ;

    // Public properties with get/set syntax
    public BuildDependency  Dependency            { get; private set; }
    public string           ParentName            { get; private set; }
    public string           ChildName             { get; private set; }
    public string           ChildUniqueName       { get; private set; }

    // Public properties with getter and setter
    public bool IsProjectDependency
    {
      get { return _IsProjectDependency ; }
      set
      {
        _IsProjectDependency = value ;
        NotifyPropertyChanged("IsProjectDependency");
      }
    }

    public bool IsReference
    {
      get { return _IsReference ; }
      set
      {
        _IsReference = value ;
        NotifyPropertyChanged("IsReference");
      }
    }

    // Constructor
    public ReferenceItem ( BuildDependency parentDependency, Project parent, Project child, bool IsBuildRef, bool IsRef )
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      Dependency          = parentDependency ;
      ParentName          = parent.Name ;
      ChildName           = child.Name ;
      ChildUniqueName     = child.UniqueName ;
      IsProjectDependency = IsBuildRef ;
      IsReference         = IsRef ;
    }

	// Utility function to fire the PropertyChanged event.
    protected virtual void NotifyPropertyChanged ( String propertyName )
	{
	  if ( ( this.PropertyChanged != null ) )
	  {
		this.PropertyChanged ( this, new PropertyChangedEventArgs(propertyName) ) ;
	  }
	}
  }
}
