﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nether.Data.PlayerManagement;
using Nether.Web.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using Nether.Web.Features.PlayerManagement.Models.PlayerManagement;

//TO DO: The group and player Image type is not yet implemented. Seperate methods need to be implemented to upload a player or group image
//TODO: Add versioning support

namespace Nether.Web.Features.PlayerManagement
{
    /// <summary>
    /// Player management admin API
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    [Route("admin")]
    public class GroupAdminController : Controller
    {
        private readonly IPlayerManagementStore _store;
        private readonly ILogger _logger;

        public GroupAdminController(IPlayerManagementStore store, ILogger<GroupAdminController> logger)
        {
            _store = store;
            _logger = logger;
        }


        /// <summary>
        /// Gets the list of group a player belongs to.
        /// </summary>
        /// <param name="gamertag">Player's gamertag.</param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GroupListResponseModel))]
        [HttpGet("players/{gamertag}/groups")]
        public async Task<ActionResult> GetPlayerGroups(string gamertag)
        {
            // Call data store
            var groups = await _store.GetPlayersGroupsAsync(gamertag);

            // Return result
            return Ok(GroupListResponseModel.FromGroups(groups));
        }


        /// <summary>
        /// Adds player to a group.
        /// </summary>
        /// <param name="gamertag">Player's gamer tag</param>
        /// <param name="groupName">Group name.</param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HttpPut("players/{gamertag}/groups/{groupName}")]
        public async Task<ActionResult> AddPlayerToGroup(string gamertag, string groupName)
        {
            Group group = await _store.GetGroupDetailsAsync(groupName);
            if (group == null)
            {
                _logger.LogWarning("group '{0}' not found", groupName);
                return BadRequest();
            }

            Player player = await _store.GetPlayerDetailsByGamertagAsync(gamertag);
            if (player == null)
            {
                _logger.LogWarning("player '{0}' not found", gamertag);
                return BadRequest();
            }

            await _store.AddPlayerToGroupAsync(group, player);

            return NoContent();
        }



        /// <summary>
        /// Removes a player from a group.
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="gamertag">Player name</param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [HttpDelete("groups/{groupName}/players/{gamertag}")]
        public async Task<ActionResult> DeletePlayerFromGroup(string groupName, string gamertag)
        {
            Player player = await _store.GetPlayerDetailsByGamertagAsync(gamertag);
            Group group = await _store.GetGroupDetailsAsync(groupName);

            await _store.RemovePlayerFromGroupAsync(group, player);

            return NoContent();
        }

        //Implementation of the group API

        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="group">Group object</param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [HttpPost("groups")]
        public async Task<ActionResult> PostGroup([FromBody]GroupPostRequestModel group)
        {
            if (!ModelState.IsValid)
            {
                var messages = new List<string>();
                var states = ModelState.Where(mse => mse.Value.Errors.Count > 0);
                foreach (var state in states)
                {
                    var message = $"{state.Key}. Errors: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}\n";
                    messages.Add(message);
                }
                _logger.LogError(string.Join("\n", messages));
                return BadRequest();
            }
            // Save group
            await _store.SaveGroupAsync(
                new Group
                {
                    Name = group.Name,
                    CustomType = group.CustomType,
                    Description = group.Description,
                    Members = group.Members
                }
            );

            // Return result
            return CreatedAtRoute(nameof(GetGroup), new { groupName = group.Name }, null);
        }

        /// <summary>
        /// Get list of all groups. You must be an administrator to perform this action.
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GroupListResponseModel))]
        [HttpGet("groups")]
        public async Task<ActionResult> GetGroupsAsync()
        {
            // Call data store
            var groups = await _store.GetGroupsAsync();

            // Return result
            return Ok(GroupListResponseModel.FromGroups(groups));
        }

        /// <summary>
        /// Gets group by name.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GroupGetResponseModel))]
        [HttpGet("groups/{groupName}", Name = nameof(GetGroup))]
        public async Task<ActionResult> GetGroup(string groupName)
        {
            // Call data store
            var group = await _store.GetGroupDetailsAsync(groupName);
            if (group == null)
            {
                return NotFound();
            }

            // Format response model
            var resultModel = new GroupGetResponseModel
            {
                Group = group
            };

            // Return result
            return Ok(resultModel);
        }

        /// <summary>
        /// Gets the members of the group as player objects.
        /// </summary>
        /// <param name="groupName">Name of the group</param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GroupMemberResponseModel))]
        [HttpGet("groups/{groupName}/players")]
        public async Task<ActionResult> GetGroupPlayers(string groupName)
        {
            // Call data store
            List<string> gamertags = await _store.GetGroupPlayersAsync(groupName);

            // Format response model
            var resultModel = new GroupMemberResponseModel
            {
                Gamertags = gamertags
            };

            // Return result
            return Ok(resultModel);
        }

        /// <summary>
        /// Updates group information
        /// </summary>
        /// <param name="group">Group name</param>
        /// <returns></returns>
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [HttpPut("groups/{name}")]
        public async Task<ActionResult> PutGroup([FromBody]GroupPostRequestModel group)
        {
            // Update group
            await _store.SaveGroupAsync(
                    new Group
                    {
                        Name = group.Name,
                        CustomType = group.CustomType,
                        Description = group.Description,
                        Members = group.Members
                    }
                );

            // Return result
            return NoContent();
        }
    }
}
