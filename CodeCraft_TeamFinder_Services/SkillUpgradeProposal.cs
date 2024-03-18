﻿using CodeCraft_TeamFinder_Models;
using CodeCraft_TeamFinder_Services.Interfaces;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCraft_TeamFinder_Services
{
    [DisallowConcurrentExecution]
    public class SkillUpgradeProposal : IJob
    {
        private readonly Lazy<ISystemRoleService> _systemRoleService;
        private readonly Lazy<IUserService> _userService;
        private readonly Lazy<IProjectService> _projectService;

        public SkillUpgradeProposal(Lazy<ISystemRoleService> systemRoleService, Lazy<IUserService> userService, Lazy<IProjectService> projectService)
        {
            _systemRoleService = systemRoleService;
            _userService = userService;
            _projectService = projectService;
        }

        private int DifferenceInMonths(DateTime startDate, DateTime endDate)
        {
            int years = endDate.Year - startDate.Year;
            int months = endDate.Month - startDate.Month;

            if (endDate.Day < startDate.Day)
            {
                months--;
            }

            int totalMonths = years * 12 + months;

            return totalMonths;
        }

        public async Task CreateSkillUpgradeProposal()
        {
            var systemRole = (await _systemRoleService.Value.Find("Name", "Employee")).First();

            if (systemRole != null)
            {
                var users = (await _userService.Value.GetAll()).Where(x => x.SystemRoleIDs != null && x.SystemRoleIDs.Contains(systemRole.Id)).ToList();

                foreach (var user in users ?? Enumerable.Empty<User>())
                {
                    var projects = (await _projectService.Value.GetEmployeeProjects(user.Id)).CurrentProjects;

                    foreach (var project in projects ?? Enumerable.Empty<ProjectInformation>())
                    {
                        var projectDetails = await _projectService.Value.Get(project.ProjectID);

                        if (projectDetails != null)
                        {
                            var skillRequirements = projectDetails.SkillRequirements?.Select(x => x.SkillID).ToList();

                            if ((projectDetails.StartDate - DateTime.Now).TotalHours == 5)
                            {
                                if (skillRequirements != null && skillRequirements.Count() > 0 && user.Skills != null && user.Skills.Count() > 0)
                                {
                                    var commonSkills = user.Skills.Select(x => x.SkillID).Intersect(skillRequirements).ToList();

                                    if (commonSkills.Count() > 0)
                                    {
                                        foreach (var skill in commonSkills)
                                        {
                                            var skillUpgrade = user.Skills.Where(x => x.SkillID == skill).FirstOrDefault();

                                            var index = user.Skills.IndexOf(skillUpgrade);

                                            user.Skills.Remove(skillUpgrade);

                                            skillUpgrade.Level = skillUpgrade.Level == "Learns" ? "Knows" : skillUpgrade.Level == "Knows" ? "Does" : skillUpgrade.Level == "Does" ? "Helps" : skillUpgrade.Level == "Helps" ? "Teaches" : "Teaches";

                                            skillUpgrade.Status = "Accepted";

                                            user.Skills.Insert(index, skillUpgrade);

                                            await _userService.Value.Update(user);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var skill in skillRequirements)
                                        {
                                            Skills addSkill = new Skills { SkillID = skill, Status = "Accepted", Level = "Learns", Experience = "0-6 months" };

                                            user.Skills.Add(addSkill);

                                            await _userService.Value.Update(user);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await this.CreateSkillUpgradeProposal();
        }

    }
}