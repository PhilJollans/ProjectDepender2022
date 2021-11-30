using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
// Both EnvDTE and Systems.Window include a Window class.
// Use an alias for the class in System.Windows
using WpfWindow = System.Windows.Window;

namespace ProjectDepender
{
  public class DependenciesViewModel
  {
    // Public properties
    public ObservableCollection<ReferenceItem>  ReferenceItems    { get; private set; } = new ObservableCollection<ReferenceItem>() ;
    public ICommand                             UpdateCommand     { get; private set; }
    public ICommand                             RegenerateCommand { get; private set; }
    public ICommand                             CloseCommand      { get; private set; }

    // Private variables
    private DTE   _dte ;

    public DependenciesViewModel ( DTE dte_object )
    {
      // Store parameters
      _dte = dte_object ;

      // Create command objects
      UpdateCommand     = new RelayCommand ( new Action<object>(Execute_UpdateCommand) ) ;
      RegenerateCommand = new RelayCommand ( new Action<object>(Execute_RegenerateCommand) ) ;
      CloseCommand      = new RelayCommand ( new Action<object>(Execute_CloseCommand) ) ;
    }

    private void Execute_RegenerateCommand ( object obj )
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      // Clear the existing BuildDependencies
      foreach ( BuildDependency dep in _dte.Solution.SolutionBuild.BuildDependencies )
      {
        dep.RemoveAllProjects() ;
      }

      foreach ( var refitem in ReferenceItems )
      {
        refitem.IsProjectDependency = false ;
      }

      // Continue with the update command
      Execute_UpdateCommand ( obj ) ;
    }

    private void Execute_UpdateCommand ( object obj )
    {
      ThreadHelper.ThrowIfNotOnUIThread();

      // Loop over the reference items
      foreach ( var refitem in ReferenceItems )
      {
        if ( refitem.IsReference && !refitem.IsProjectDependency )
        {
          refitem.Dependency.AddProject ( refitem.ChildUniqueName ) ;
          refitem.IsProjectDependency = true ;
        }
      }
    }

    private void Execute_CloseCommand (object obj)
    {
      var win = obj as WpfWindow ;
      win.Close();
    }

  }
}
