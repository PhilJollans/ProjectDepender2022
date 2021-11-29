//------------------------------------------------------------------------------
// <copyright file="ProjectDependencies.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Linq;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using VSLangProj;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace ProjectDepender
{
  /// <summary>
  /// Command handler
  /// </summary>
  internal sealed class ProjectDependencies
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x0100;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("be92087d-77ad-4325-b413-ec0139a64587");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly Package package;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDependencies"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    private ProjectDependencies(Package package)
    {
      if (package == null)
      {
        throw new ArgumentNullException("package");
      }

      this.package = package;

      OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
      if (commandService != null)
      {
        var menuCommandID = new CommandID(CommandSet, CommandId);
        var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
        commandService.AddCommand(menuItem);
      }
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static ProjectDependencies Instance
    {
      get;
      private set;
    }

    /// <summary>
    /// Gets the service provider from the owner package.
    /// </summary>
    private IServiceProvider ServiceProvider
    {
      get
      {
        return this.package;
      }
    }

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static void Initialize(Package package)
    {
      Instance = new ProjectDependencies(package);
    }

    /// <summary>
    /// This function is the callback used to execute the command when the menu item is clicked.
    /// See the constructor to see how the menu item is associated with this function using
    /// OleMenuCommandService service and MenuCommand class.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event args.</param>
    private void MenuItemCallback(object sender, EventArgs e)
    {
      //string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
      //string title = "ProjectDependencies";

      //// Show a message box to prove we were here
      //VsShellUtilities.ShowMessageBox(
      //    this.ServiceProvider,
      //    message,
      //    title,
      //    OLEMSGICON.OLEMSGICON_INFO,
      //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
      //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

      try
      {
        DTE dte_object = (package as ProjectDependenciesPackage).dte_object ;

        var vm = new DependenciesViewModel ( dte_object ) ;

        // Version 1.3
        // Get a flat list of projects, recursing into solution folders
        var ProjectList = GetFlatListOfProjects ( dte_object.Solution ) ;

        // Version 1.1
        // Start by building a list of project dependencies which we can show in a grid.
        foreach ( BuildDependency dep in dte_object.Solution.SolutionBuild.BuildDependencies )
        {
          // Build a list of references, which we will use below
          var   refs   = new List<string>() ;
          var   prj    = dep.Project ;
          var   vsproj = prj.Object as VSProject ;

          if ( vsproj != null )
          {
            foreach ( Reference refer in vsproj.References )
            {
              string  UsedName   = refer.Name ;

              // Ad hoc rules to handle interop components
              if ( UsedName.StartsWith ( "Interop." ) )
              {
                UsedName = UsedName.Substring(8) ;

                if ( UsedName.EndsWith ( "Lib" ) )
                {
                  UsedName = UsedName.Substring ( 0, UsedName.Length-3 ) ;
                }
              }

              // Add to the list
              refs.Add ( UsedName ) ;
            }
          }

          // Loop over all of the projects and determine if the 'parent' project
          // has a build dependency or a reference to each one
          foreach ( Project p2 in ProjectList )
          {
            var deps              = dep.RequiredProjects as IEnumerable ;
            var IsBuildDependency = deps.Cast<Project>().Contains ( p2 ) ;
            var IsReference       = refs.Contains ( p2.Name ) ;

            Debug.WriteLine ( "{0},{1},{2},{3}", prj.Name, p2.Name, IsBuildDependency, IsReference ) ;

            if ( IsBuildDependency || IsReference )
            {
              // Add an reference item to show
              vm.ReferenceItems.Add ( new ReferenceItem ( dep, prj, p2, IsBuildDependency, IsReference ) ) ;
            }
          }
        }

        if ( vm.ReferenceItems.Count  == 0 )
        {
          MessageBox.Show ( "No project dependencies or references found." ) ;
        }
        else
        {
          var dv = new DependenciesView ( vm ) ;
          dv.ShowDialog() ;
        }

      }
      catch ( Exception ex )
      {
        string msg = string.Format ( "(0)\n{1}\n{2}", ex.GetType().ToString(), ex.Message, ex.StackTrace ) ;
        VsShellUtilities.ShowMessageBox(
            this.ServiceProvider,
            msg,
            "Project Depender",
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

      }
    }

    private List<Project> GetFlatListOfProjects ( Solution s )
    {
      var results = new List<Project>() ;

      foreach ( Project p in s.Projects )
      {
        if ( p.Kind == EnvDTE.Constants.vsProjectKindSolutionItems )
          ProcessSolutionFolder ( p, results ) ;
        else
          results.Add ( p ) ;
      }
      return results ;
    }

    private void ProcessSolutionFolder ( Project p, List<Project> results )
    {
      foreach ( ProjectItem pi in p.ProjectItems )
      {
        Project pp = pi.Object as Project;
        if ( pp != null )
        {
          if (pp.Kind == EnvDTE.Constants.vsProjectsKindSolution )
            ProcessSolutionFolder ( pp, results ) ;
          else
            results.Add ( pp ) ;
        }
      }
    }

  }
}
