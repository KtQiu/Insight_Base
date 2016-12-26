﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Insight.Base.Common;
using Insight.Base.Common.Entity;
using Insight.Utils.Common;
using Insight.Utils.Entity;

namespace Insight.Base.Services
{
    public partial class UserGroups
    {
        /// <summary>
        /// 根据对象实体数据新增一个用户组
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="obj">用户组对象</param>
        /// <returns>object 插入的用户组ID </returns>
        private object InsertData(Guid id, SYS_UserGroup obj)
        {
            var helper = new SqlHelper(Parameters.Database);
            const string sql = "insert into SYS_UserGroup (Name, Description, CreatorUserId) select @Name, @Description, @CreatorUserId select ID From SYS_UserGroup where SN = SCOPE_IDENTITY()";
            var parm = new[]
            {
                new SqlParameter("@Name", obj.Name),
                new SqlParameter("@Description", obj.Description),
                new SqlParameter("@CreatorUserId", SqlDbType.UniqueIdentifier) {Value = id}
            };
            return helper.SqlScalar(sql, parm);
        }

        /// <summary>
        /// 根据ID删除用户组
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>bool 是否删除成功</returns>
        private bool DeleteGroup(Guid id)
        {
            var helper = new SqlHelper(Parameters.Database);
            var sql = $"Delete from SYS_UserGroup where ID = '{id}' and BuiltIn = 0";
            return helper.SqlNonQuery(sql) > 0;
        }

        /// <summary>
        /// 根据对象实体数据更新用户组信息
        /// </summary>
        /// <param name="obj">用户组对象</param>
        /// <returns>bool 是否更新成功</returns>
        private bool Update(SYS_UserGroup obj)
        {
            var helper = new SqlHelper(Parameters.Database);
            const string sql = "update SYS_UserGroup set Name = @Name, Description = @Description where ID = @ID";
            var parm = new[]
            {
                new SqlParameter("@ID", SqlDbType.UniqueIdentifier) {Value = obj.ID},
                new SqlParameter("@Name", obj.Name),
                new SqlParameter("@Description", obj.Description)
            };
            return helper.SqlNonQuery(sql, parm) > 0;
        }

        /// <summary>
        /// 根据ID获取用户组对象实体
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>SYS_UserGroup 用户组对象</returns>
        private SYS_UserGroup GetGroup(Guid id)
        {
            using (var context = new BaseEntities())
            {
                return context.SYS_UserGroup.SingleOrDefault(e => e.ID == id);
            }
        }

        /// <summary>
        /// 获取全部用户组
        /// </summary>
        /// <returns>DataTable 全部用户组结果集</returns>
        private IEnumerable<object> GetGroupList()
        {
            using (var context = new BaseEntities())
            {
                var list = context.SYS_UserGroup.Where(g => g.Visible).OrderBy(g => g.SN).ToList();
                return list.Select(g => new {g.ID, g.BuiltIn, g.Name, g.Description});
            }
        }

        /// <summary>
        /// 根据参数组集合批量插入用户组成员关系
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="gid">用户组ID</param>
        /// <param name="uids">用户ID集合</param>
        /// <returns>bool 是否插入成功</returns>
        private bool AddGroupMember(Guid id, Guid gid, IEnumerable<Guid> uids)
        {
            var helper = new SqlHelper(Parameters.Database);
            const string sql = "insert into SYS_UserGroupMember (GroupId, UserId, CreatorUserId) select @GroupId, @UserId, @CreatorUserId";
            var cmds = uids.Select(uid => new[]
            {
                new SqlParameter("@GroupId", SqlDbType.UniqueIdentifier) {Value = gid},
                new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) {Value = uid},
                new SqlParameter("@CreatorUserId", SqlDbType.UniqueIdentifier) {Value = id}
            }).Select(parm => helper.MakeCommand(sql, parm)).ToList();
            return helper.SqlExecute(cmds);
        }

        /// <summary>
        /// 根据ID集合删除用户组成员关系
        /// </summary>
        /// <param name="ids">户组成员关系ID集合</param>
        /// <returns>bool 是否删除成功</returns>
        private bool DeleteMember(IEnumerable<Guid> ids)
        {
            var helper = new SqlHelper(Parameters.Database);
            const string sql = "Delete from SYS_UserGroupMember where ID = @ID";
            var cmds = ids.Select(id => new[]
            {
                new SqlParameter("@ID", SqlDbType.UniqueIdentifier) {Value = id}
            }).Select(parm => helper.MakeCommand(sql, parm)).ToList();
            return helper.SqlExecute(cmds);
        }

        /// <summary>
        /// 获取全部用户组的所有成员信息
        /// </summary>
        /// <returns>DataTable 全部用户组成员信息结果集</returns>
        private IEnumerable<object> GetMemberList()
        {
            using (var context = new BaseEntities())
            {
                var list = from u in context.SYS_User.OrderBy(u => u.SN)
                           join m in context.SYS_UserGroupMember on u.ID equals m.UserId
                           join g in context.SYS_UserGroup on m.GroupId equals g.ID
                           where u.Validity && g.Visible
                           select new {m.ID, m.GroupId, m.UserId, u.Name, u.LoginName, u.Description};
                return list.ToList();
            }
        }

        /// <summary>
        /// 根据ID获取组成员之外的全部用户
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>DataTable 组成员之外所有用户信息结果集</returns>
        private IEnumerable<object> GetOtherUser(Guid id)
        {
            using (var context = new BaseEntities())
            {
                var members = from m in context.SYS_UserGroupMember where m.GroupId == id select m.UserId;
                var list = from u in context.SYS_User
                           where u.Validity && u.Type > 0 && !members.Any(m => m == u.ID)
                           select new { u.ID, u.Name, u.LoginName };
                return list.OrderBy(u => u.LoginName).ToList();
            }
        }

    }
}