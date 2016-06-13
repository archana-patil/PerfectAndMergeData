using PerfectAndMergeData.Common;
using PerfectAndMergeData.DTO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;


namespace PerfectAndMergeData
{
    public class PerfectAndMergeData
    {
        private SqlConnection _connection = null;
        private string sqlConnString;

        public PerfectAndMergeData(SqlConnection Conn)
        {
            _connection = Conn;
        }

        public PerfectAndMergeData(string connString)
        {
            sqlConnString = connString;
        }

        public DateTime ConvertTimeToUTC(DateTime time, string TimeZoneId)
        {
            DateTime time1 = DateTime.Parse(time.ToString());   //("2012.12.04T08:35:00");
            TimeZoneInfo objTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            DateTime dtUTC = TimeZoneInfo.ConvertTimeToUtc(time1, objTimeZoneInfo);
            return dtUTC;
        }

        public DateTime ConvertTimeFromUTC(DateTime utcTime, string TimeZoneId)
        {
            TimeZoneInfo objTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            DateTime dateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, objTimeZoneInfo);
            return dateTime;
        }

        #region Session Details

        public SessionListResultSet GetSessions()
        {
            List<PAM2Session> lstSession = new List<PAM2Session>();
            SessionListResultSet sessionResult = new SessionListResultSet();
            try
            {
                TraceLog.Write("Start " + MethodBase.GetCurrentMethod().Name);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                lstSession = (from entitySetting in pam2EntitiesContext.EntitySettings
                              join session in pam2EntitiesContext.Sessions on entitySetting.EntitySettingId equals session.EntitySettingId
                              join status in pam2EntitiesContext.Status on session.MergeStatus equals status.StatusId
                              join user in pam2EntitiesContext.Users on session.CreatedBy equals user.UserId
                              join matchDetail in pam2EntitiesContext.SessionMatchDetails on session.SessionId equals matchDetail.SessionId
                              into sessionList
                              from finalSession in sessionList.DefaultIfEmpty()
                              where (session.IsDeleted != null && session.IsDeleted == false)
                              select new
                              {
                                  SessionId = session.SessionId,
                                  SessionName = session.SessionName,
                                  EntitySettingId = session.EntitySettingId,
                                  Entity = entitySetting.EntityLogicalName,
                                  StatusId = session.MergeStatus,
                                  Status = status.Name,
                                  CreatedDate = session.CreatedDate, // ConvertTimeFromUTC(Convert.ToDateTime(session.CreatedDate),TimeZoneId),
                                  GroupCount = session.GroupCount,
                                  RecordsFed = finalSession.RecordsFed == null ? 0 : finalSession.RecordsFed,
                                  DuplicatesFound = finalSession.DuplicatesFound == null ? 0 : finalSession.DuplicatesFound,
                                  ConfirmedResultCount = finalSession.ConfirmedResultCount == null ? 0 : finalSession.ConfirmedResultCount,
                                  UnsureResultCount = finalSession.UnsureResultCount == null ? 0 : finalSession.UnsureResultCount,
                                  CreatedBy = user.FirstName + " " + user.LastName,
                                  IsDeactivated = session.IsDeactivated,
                                  MatchStatus = finalSession.MatchStatus,
                              }).AsEnumerable().Select(session => new PAM2Session
                                {
                                    SessionId = session.SessionId,
                                    SessionName = session.SessionName,
                                    EntitySettingId = session.EntitySettingId.ToString(),
                                    Entity = session.Entity,
                                    StatusId = session.StatusId.ToString(),
                                    Status = session.Status,
                                    CreatedDate = session.CreatedDate.ToString(),
                                    GroupCount = session.GroupCount != null ? session.GroupCount.ToString() : "0",
                                    RecordsFed = session.RecordsFed.ToString(),
                                    DuplicatesFound = session.DuplicatesFound.ToString(),
                                    ConfirmedResultCount = session.ConfirmedResultCount.ToString(),
                                    UnsureResultCount = session.UnsureResultCount.ToString(),
                                    CreatedBy = session.CreatedBy,
                                    IsDeactivated = Convert.ToBoolean(session.IsDeactivated),
                                    MatchStatus = session.MatchStatus
                                }).ToList<PAM2Session>();
                sessionResult.Message = "Success";
                sessionResult.Sessions = lstSession;
                sessionResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            TraceLog.Write("End " + MethodBase.GetCurrentMethod().Name);
            return sessionResult;
        }

        public ResultSet DeleteSession(string SessionId)
        {
            ResultSet sessionResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Session session = null;
                Guid sessionIdGuid = new Guid(SessionId);
                session = pam2EntitiesContext.Sessions.Where(s => s.SessionId == sessionIdGuid).FirstOrDefault();
                if (session != null)
                {
                    session.IsDeleted = true;
                    int count = pam2EntitiesContext.SaveChanges();
                    sessionResult.Message = "Success";
                    sessionResult.Result = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return sessionResult;
        }

        public ResultSet ChangeStatusofSession(string SessionId)
        {
            ResultSet sessionResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Session session = null;
                Guid sessionIdGuid = new Guid(SessionId);
                session = pam2EntitiesContext.Sessions.Where(s => s.SessionId == sessionIdGuid).FirstOrDefault();
                if (session != null)
                {
                    session.IsDeactivated = !session.IsDeactivated;
                    int count = pam2EntitiesContext.SaveChanges();
                    sessionResult.Message = "Success";
                    sessionResult.Result = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return sessionResult;
        }

        public Session CreateNewSession(string SessionName, string EntitSettingId, Guid pamUserId)
        {
            Session addedSession = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Status objStatus = pam2EntitiesContext.Status.Where(c => c.Enum == "MP").FirstOrDefault<Status>();

                if (!string.IsNullOrWhiteSpace(SessionName) && !string.IsNullOrWhiteSpace(EntitSettingId))
                {
                    Session newSession = new Session();
                    newSession.EntitySettingId = new Guid(EntitSettingId);
                    newSession.GroupCount = 0;
                    newSession.IsAutoMergeInProgress = false;
                    newSession.IsDeleted = false;
                    newSession.MergeStatus = objStatus.StatusId;
                    newSession.RefreshDate = DateTime.UtcNow;
                    newSession.SessionName = SessionName;
                    //this field need to be taken from extjs and will be sent across request and applied
                    newSession.CreatedBy = pamUserId;
                    newSession.IsDeactivated = false;
                    newSession.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.Sessions.Add(newSession);
                    pam2EntitiesContext.SaveChanges();
                    addedSession = newSession;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSession;
        }

        #endregion

        #region Custom Transform Categories

        public CTLCategoriesListResultSet GetCTLCategories()
        {
            List<CTLCategory> lstCTLCategory = new List<CTLCategory>();
            CTLCategoriesListResultSet cTLCategoriesResult = new CTLCategoriesListResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                lstCTLCategory = (from category in pam2EntitiesContext.CategoryMasters
                                  where category.IsMaster == true
                                  orderby category.Category ascending
                                  select new
                                  {
                                      CategoryId = category.CategoryId,
                                      Category = category.Category
                                  }).AsEnumerable().Select(category => new CTLCategory
                                  {
                                      CategoryId = category.CategoryId.ToString(),
                                      Category = category.Category,
                                  }).ToList<CTLCategory>();

                cTLCategoriesResult.Message = "Success";
                cTLCategoriesResult.CTLCategories = lstCTLCategory;
                cTLCategoriesResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return cTLCategoriesResult;
        }

        public bool CheckIfCategoryExistsInManageGroupRules(string CategoryId)
        {
            bool bIsExists = false;
            Guid gCategoryId = new Guid(CategoryId);
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                GroupRuleDetail obGroupRuleDetail = pam2EntitiesContext.GroupRuleDetails.Where(c => c.AttributeValue == CategoryId).FirstOrDefault();

                if (obGroupRuleDetail != null)
                    return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return bIsExists;
        }

        public ResultSet DeleteCTLCategory(string categoryId)
        {
            ResultSet categoryResult = new ResultSet();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                CategoryMaster categoryMaster = null;
                Guid categoryMasterIdGuid = new Guid(categoryId);

                List<CategoryDetail> lstCategoryDetail = pam2EntitiesContext.CategoryDetails.Where(s => s.CategoryId == categoryMasterIdGuid).ToList<CategoryDetail>();

                foreach (CategoryDetail objCategoryDetail in lstCategoryDetail)
                {
                    DeleteCTLCategoryDetail(objCategoryDetail.CategoryDetaild.ToString());
                }

                pam2EntitiesContext = new PAM2Entities(sqlConnString);
                categoryMaster = pam2EntitiesContext.CategoryMasters.Where(s => s.CategoryId == categoryMasterIdGuid).FirstOrDefault();
                if (categoryMaster != null)
                {
                    pam2EntitiesContext.CategoryMasters.Remove(categoryMaster);
                    int count = pam2EntitiesContext.SaveChanges();
                    categoryResult.Message = "Success";
                    categoryResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                categoryResult.Message = excObj.Message;
                categoryResult.Result = false;
                throw excObj;
            }
            return categoryResult;
        }

        public ResultSet AddCTLCategory(string Category, bool IsMaster, string CreatedBy, out CategoryMaster CreatedRecord)
        {
            ResultSet categorypResult = new ResultSet();
            CategoryMaster addedCategory = null;
            CategoryMaster newCategory = new CategoryMaster();
            CreatedRecord = null;
            try
            {
                newCategory.Category = Category;
                newCategory.IsMaster = IsMaster;
                newCategory.CreatedBy = new Guid(CreatedBy);
                newCategory.CreatedDate = DateTime.UtcNow;
                if (newCategory.CategoryId == Guid.Empty)
                    newCategory.CategoryId = Guid.NewGuid();

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                addedCategory = pam2EntitiesContext.CategoryMasters.Add(newCategory);
                pam2EntitiesContext.SaveChanges();

                if (addedCategory != null)
                {
                    CreatedRecord = addedCategory;
                    categorypResult.Message = "Success";
                    categorypResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return categorypResult;
        }

        public ResultSet UpdateCTLCategory(string categoryId, string Category, string updatedBy)
        {
            ResultSet categorypResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                CategoryMaster category = null;
                Guid categoryIdGuid = new Guid(categoryId);
                category = pam2EntitiesContext.CategoryMasters.Where(s => s.CategoryId == categoryIdGuid).FirstOrDefault();
                if (category != null)
                {
                    category.Category = Category;
                    category.UpdateDate = DateTime.UtcNow;
                    // matchGroup.UpdatedBy = new Guid(updatedBy);
                    int count = pam2EntitiesContext.SaveChanges();

                    categorypResult.Message = "Success";
                    categorypResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return categorypResult;
        }



        public CTLCategoryDetailListResultSet GetCTLCategoryDetail(string CategoryId = "")
        {
            List<CTLCategoryDetail> lstCTLCategoryDetail = new List<CTLCategoryDetail>();
            CTLCategoryDetailListResultSet cTLCategoriesResult = new CTLCategoryDetailListResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                if (String.IsNullOrEmpty(CategoryId.Trim()))
                {
                    lstCTLCategoryDetail = (from category in pam2EntitiesContext.CategoryDetails
                                            where category.CategoryMaster.IsMaster == true
                                            orderby category.CategoryMaster.Category ascending
                                            select new
                                            {
                                                CategoryDetaild = category.CategoryDetaild,
                                                CategoryId = category.CategoryId,
                                                FromText = category.FromText,
                                                ToText = category.ToText
                                            }).AsEnumerable().Select(category => new CTLCategoryDetail
                                      {
                                          CategoryDetaild = category.CategoryDetaild.ToString(),
                                          CategoryId = category.CategoryId.ToString(),
                                          FromText = category.FromText,
                                          ToText = category.ToText
                                      }).ToList<CTLCategoryDetail>();

                    cTLCategoriesResult.Message = "Success";
                    cTLCategoriesResult.CTLCategoryDetails = lstCTLCategoryDetail;
                    cTLCategoriesResult.Result = true;
                }
                else
                {
                    Guid gCategoryId = new Guid(CategoryId);
                    lstCTLCategoryDetail = (from category in pam2EntitiesContext.CategoryDetails
                                            where category.CategoryId == gCategoryId
                                            orderby category.CategoryMaster.Category ascending
                                            select new
                                            {
                                                CategoryDetaild = category.CategoryDetaild,
                                                CategoryId = category.CategoryId,
                                                FromText = category.FromText,
                                                ToText = category.ToText
                                            }).AsEnumerable().Select(category => new CTLCategoryDetail
                                            {
                                                CategoryDetaild = category.CategoryDetaild.ToString(),
                                                CategoryId = category.CategoryId.ToString(),
                                                FromText = category.FromText,
                                                ToText = category.ToText
                                            }).ToList<CTLCategoryDetail>();

                    cTLCategoriesResult.Message = "Success";
                    cTLCategoriesResult.CTLCategoryDetails = lstCTLCategoryDetail;
                    cTLCategoriesResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }

            return cTLCategoriesResult;
        }

        public ResultSet DeleteCTLCategoryDetail(string categoryDetailId)
        {
            ResultSet categoryResult = new ResultSet();
            try
            {

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                CategoryDetail categoryDetail = null;
                Guid categoryDetailIdGuid = new Guid(categoryDetailId);
                categoryDetail = pam2EntitiesContext.CategoryDetails.Where(s => s.CategoryDetaild == categoryDetailIdGuid).FirstOrDefault();
                if (categoryDetail != null)
                {
                    pam2EntitiesContext.CategoryDetails.Remove(categoryDetail);
                    int count = pam2EntitiesContext.SaveChanges();
                    categoryResult.Message = "Success";
                    categoryResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                categoryResult.Message = excObj.Message;
                categoryResult.Result = false;
                throw excObj;
            }
            return categoryResult;
        }

        public ResultSet AddCTLCategoryDetail(string CategoryId, string FromText, string ToText, string CreatedBy)
        {
            ResultSet categorypResult = new ResultSet();
            CategoryDetail addedCategory = null;
            CategoryDetail newCategory = new CategoryDetail();
            try
            {
                newCategory.CategoryId = new Guid(CategoryId);
                newCategory.FromText = FromText;
                newCategory.ToText = ToText;
                newCategory.CreatedBy = new Guid(CreatedBy);
                newCategory.CreatedDate = DateTime.UtcNow;
                if (newCategory.CategoryDetaild == Guid.Empty)
                    newCategory.CategoryDetaild = Guid.NewGuid();

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                addedCategory = pam2EntitiesContext.CategoryDetails.Add(newCategory);
                pam2EntitiesContext.SaveChanges();

                if (addedCategory != null)
                {
                    categorypResult.Message = "Success";
                    categorypResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return categorypResult;
        }

        public ResultSet UpdateCTLCategoryDetail(string categoryDetailId, string CategoryId, string FromText, string ToText, string updatedBy)
        {
            ResultSet categorypResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                CategoryDetail category = null;
                Guid gcategoryDetailId = new Guid(categoryDetailId);
                category = pam2EntitiesContext.CategoryDetails.Where(s => s.CategoryDetaild == gcategoryDetailId).FirstOrDefault();

                if (category != null)
                {
                    category.CategoryId = new Guid(CategoryId);
                    category.FromText = FromText;
                    category.ToText = ToText;
                    category.UpdateDate = DateTime.UtcNow;
                    category.UpdateBy = new Guid(updatedBy);

                    int count = pam2EntitiesContext.SaveChanges();

                    categorypResult.Message = "Success";
                    categorypResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }

            return categorypResult;
        }

        #endregion

        public MatchGroupListResultSet GetMasterMatchGroups()
        {
            List<PAM2MatchGroup> lstMatchGroups = new List<PAM2MatchGroup>();
            MatchGroupListResultSet matchGroupResult = new MatchGroupListResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                lstMatchGroups = (from matchGroup in pam2EntitiesContext.MatchGroupMasters
                                  orderby matchGroup.Name ascending
                                  select new
                                  {
                                      MatchGroupId = matchGroup.MatchGroupId,
                                      Name = matchGroup.Name
                                  }).AsEnumerable().Select(matchGroup => new PAM2MatchGroup
                                    {
                                        MatchGroupId = matchGroup.MatchGroupId.ToString(),
                                        Name = matchGroup.Name,
                                    }).ToList<PAM2MatchGroup>();
                matchGroupResult.Message = "Success";
                matchGroupResult.MatchGroups = lstMatchGroups;
                matchGroupResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupResult;
        }

        public ResultSet AddMasterMatchGroups(string Name, string CreatedBy, out MatchGroupMaster ResultMatchGroup)
        {
            ResultSet matchGroupResult = new ResultSet();
            MatchGroupMaster addedMatchGroup = null;
            MatchGroupMaster newMatchGroup = new MatchGroupMaster();
            try
            {
                newMatchGroup.Name = Name;
                newMatchGroup.CreatedBy = new Guid(CreatedBy);
                newMatchGroup.CreatedDate = DateTime.UtcNow;
                if (newMatchGroup.MatchGroupId == Guid.Empty)
                    newMatchGroup.MatchGroupId = Guid.NewGuid();

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                addedMatchGroup = pam2EntitiesContext.MatchGroupMasters.Add(newMatchGroup);
                pam2EntitiesContext.SaveChanges();

                if (addedMatchGroup != null)
                {
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }

            ResultMatchGroup = addedMatchGroup;
            return matchGroupResult;
        }

        public ResultSet UpdateMasterMatchGroup(string matchGroupId, string Name, string updatedBy)
        {
            ResultSet matchGroupResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchGroupMaster matchGroup = null;
                Guid matchGroupIdGuid = new Guid(matchGroupId);
                matchGroup = pam2EntitiesContext.MatchGroupMasters.Where(s => s.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
                if (matchGroup != null)
                {
                    matchGroup.Name = Name;
                    matchGroup.UpdateDate = DateTime.UtcNow;
                    matchGroup.UpdatedBy = new Guid(updatedBy);
                    int count = pam2EntitiesContext.SaveChanges();
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupResult;
        }

        public bool CheckIfGroupExistsInManageGroups(string MatchGroupId)
        {
            bool bIsExists = false;
            Guid gMatchGroupId = new Guid(MatchGroupId);
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchGroup objMatchGroup = pam2EntitiesContext.MatchGroupMasters.Where(c => c.MatchGroupId == gMatchGroupId).Join(pam2EntitiesContext.MatchGroups.Where(c => c.IsMaster == true), MG => MG.Name.ToLower().Trim(), MGM => MGM.DisplayName.ToLower().Trim(), (MGM, MG) => new { MG1 = MG }).AsEnumerable().Select(c => c.MG1).FirstOrDefault();

                if (objMatchGroup != null)
                    return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return bIsExists;
        }

        public bool CheckManageGroupDependency(string MatchGroupId)
        {
            bool bIsExists = false;
            Guid gMatchGroupId = new Guid(MatchGroupId);
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                MatchGroup objMatchGroup = pam2EntitiesContext.MatchGroups.Where(c => c.MatchGroupId == gMatchGroupId && c.IsMaster == true).FirstOrDefault();
                if (objMatchGroup != null)
                {
                    MatchGroup objMatchGroup1 = pam2EntitiesContext.MatchGroups.Where(c => c.DisplayName.ToLower().Trim() == objMatchGroup.DisplayName.ToLower().Trim() && c.IsMaster == false).FirstOrDefault();

                    if (objMatchGroup1 != null)
                    {
                        Guid? MatchGroupID = pam2EntitiesContext.SessionGroups.Where(c => c.MatchGroup.DisplayName.ToLower().Trim() == objMatchGroup.DisplayName.ToLower().Trim() && c.MatchGroup.IsMaster == false && c.Session.IsDeleted == false).Select(c => c.MatchGroupId).FirstOrDefault();
                        if (MatchGroupID != null)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return bIsExists;
        }

        public ResultSet DeleteMatchGroup(string MatchGroupId)
        {
            ResultSet matchGroupResult = new ResultSet();
            try
            {
                if (CheckIfGroupExistsInManageGroups(MatchGroupId))
                {
                    matchGroupResult.success = false;
                    matchGroupResult.Message = "Can not delete this group as it is referred in Manage Groups";
                    return matchGroupResult;
                }

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchGroupMaster matchGroup = null;
                Guid matchGroupIdGuid = new Guid(MatchGroupId);
                matchGroup = pam2EntitiesContext.MatchGroupMasters.Where(s => s.MatchGroupId == matchGroupIdGuid).FirstOrDefault();

                if (matchGroup != null)
                {
                    //List<GroupRule> lstGRoupRules = pam2EntitiesContext.GroupRules.Where(c => c.GroupId == matchGroupIdGuid).ToList<GroupRule>();
                    //foreach (GroupRule objGroupRule in lstGRoupRules)
                    //{
                    //    pam2EntitiesContext.GroupRules.Remove(objGroupRule);
                    //}
                    //   pam2EntitiesContext = new PAM2Entities(sqlConnString);

                    matchGroup = pam2EntitiesContext.MatchGroupMasters.Remove(matchGroup);
                    int count = pam2EntitiesContext.SaveChanges();
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                matchGroupResult.Message = excObj.Message;
                matchGroupResult.Result = false;
                throw excObj;
            }
            return matchGroupResult;
        }

        public EntitySettingResultSet GetEntitySettings()
        {
            EntitySettingResultSet entityResultSet = new EntitySettingResultSet();
            List<PAM2EntitySetting> lstEntitySetting = new List<PAM2EntitySetting>();
            try
            {
                TraceLog.Write("Start " + MethodBase.GetCurrentMethod().Name);

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstEntitySetting = (from entitySetting in pam2EntitiesContext.EntitySettings
                                    select new
                                    {
                                        EntitySettingId = entitySetting.EntitySettingId,
                                        EntityLogicalName = entitySetting.EntityLogicalName,
                                        EntityDisplayName = entitySetting.EntityDisplayName,
                                    }).AsEnumerable().Select(entitySetting => new PAM2EntitySetting
                                    {
                                        EntitySettingId = entitySetting.EntitySettingId.ToString(),
                                        EntityLogicalName = entitySetting.EntityLogicalName,
                                        EntityDisplayName = entitySetting.EntityDisplayName
                                    }).OrderBy(p => p.EntityDisplayName).ToList<PAM2EntitySetting>();
                entityResultSet.Message = "Success";
                entityResultSet.EntitySettings = lstEntitySetting;
                entityResultSet.Result = true;
                entityResultSet.total = lstEntitySetting.Count;
            }
            catch (Exception excObj)
            {
                TraceLog.Write("Error in " + MethodBase.GetCurrentMethod().Name + ": " + excObj.ToString(), excObj);
                throw excObj;
            }
            TraceLog.Write("End " + MethodBase.GetCurrentMethod().Name);
            return entityResultSet;
        }

        public EntitySetting GetEntitySettingbyID(string EntitySettingId)
        {
            EntitySetting objEntitySetting = new EntitySetting();

            try
            {
                Guid gId = new Guid(EntitySettingId);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                 objEntitySetting = (from c in pam2EntitiesContext.EntitySettings
                                                  where c.EntitySettingId == gId
                                                  select c).FirstOrDefault<EntitySetting>();

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objEntitySetting;
        }

        //public AttributeSettingResultSet GetAttributeSettings(string entitySettingId)
        //{
        //    AttributeSettingResultSet attributeSettingResultSet = new AttributeSettingResultSet();
        //    Guid entitySettingIdGUID = new Guid(entitySettingId);
        //    List<PAMAttributeSetting> lstAttributeSetting = new List<PAMAttributeSetting>();
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        lstAttributeSetting = (from attributeSetting in pam2EntitiesContext.AttributeSettings
        //                               where attributeSetting.EntitySettingId == entitySettingIdGUID
        //                               select attributeSetting).AsEnumerable().Select(pamattributeSetting => new PAMAttributeSetting
        //                                {
        //                                    AttributeSettingId = pamattributeSetting.AttributeSettingId,
        //                                    CustomName = pamattributeSetting.CustomName,
        //                                    DisplayName = pamattributeSetting.DisplayName,
        //                                    DisplayOrder = pamattributeSetting.DisplayOrder,
        //                                    EntitySettingId = pamattributeSetting.EntitySettingId,
        //                                    ExcludeUpdate = pamattributeSetting.ExcludeUpdate,
        //                                    IsVisible = pamattributeSetting.IsVisible,
        //                                    SchemaName = pamattributeSetting.SchemaName,
        //                                    SectionId = pamattributeSetting.SectionId,
        //                                    SessionId = pamattributeSetting.SessionId,
        //                                }).ToList<PAMAttributeSetting>();
        //        attributeSettingResultSet.Message = "Success";
        //        attributeSettingResultSet.AttributeSettings = lstAttributeSetting;
        //        attributeSettingResultSet.Result = true;
        //        attributeSettingResultSet.total = lstAttributeSetting.Count;
        //    }
        //    catch (Exception excObj)
        //    {
        //        throw excObj;
        //    }
        //    return attributeSettingResultSet;
        //}

        public MatchGroupAttributeSettingResultSet GetAttributeSettings(string entitySettingId)
        {
            MatchGroupAttributeSettingResultSet attributeSettingResultSet = new MatchGroupAttributeSettingResultSet();
            Guid entitySettingIdGUID = new Guid(entitySettingId);
            List<PAMMatchGroupAttributeSetting> lstAttributeSetting = new List<PAMMatchGroupAttributeSetting>();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstAttributeSetting = (from attributeSetting in pam2EntitiesContext.AttributeSettings
                                       where attributeSetting.EntitySettingId == entitySettingIdGUID
                                       select attributeSetting).AsEnumerable().Select(pamattributeSetting => new PAMMatchGroupAttributeSetting
                                        {
                                            cls = "file",
                                            DisplayName = pamattributeSetting.DisplayName,
                                            CustomName = pamattributeSetting.CustomName,
                                            DisplayOrder = Convert.ToString(pamattributeSetting.DisplayOrder),
                                            EntitySettingId = pamattributeSetting.EntitySettingId.ToString(),
                                            ExcludeUpdate = pamattributeSetting.ExcludeUpdate,
                                            GroupName = "",
                                            id = pamattributeSetting.AttributeSettingId.ToString(),
                                            IsVisible = pamattributeSetting.IsVisible,
                                            leaf = true,
                                            MatchAttributeSettingId = "",
                                            MatchGroupId = "",
                                            qtip = pamattributeSetting.DisplayName,
                                            SchemaName = pamattributeSetting.SchemaName,
                                            SectionId = pamattributeSetting.SessionId.ToString(),
                                            SessionId = "",
                                            text = pamattributeSetting.DisplayName

                                        }).ToList<PAMMatchGroupAttributeSetting>();
                attributeSettingResultSet.Message = "Success";
                attributeSettingResultSet.MatchGroupAttributeSettings = lstAttributeSetting;
                attributeSettingResultSet.Result = true;
                attributeSettingResultSet.total = lstAttributeSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return attributeSettingResultSet;
        }

        public void RemoveFieldsinSettingsNotinMSCRM(List<string> Fields, PAM2EntitySetting objPAM2EntitySetting)
        {
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                DataTable dt = new DataTable();
                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"select * from [dbo].[AttributeSetting] where SchemaName != 'Default' and EntitySettingId = '" + objPAM2EntitySetting.EntitySettingId + "'";
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                    string strIDS = String.Empty;

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if(!Fields.Contains(Convert.ToString(dt.Rows[i]["SchemaName"])))
                        {
                          //  dt.Rows.RemoveAt(i);
                          //  i--;
                            strIDS += "'" + Convert.ToString(dt.Rows[i]["AttributeSettingId"]) + "',";
                        }
                    }

                    if (!String.IsNullOrEmpty(strIDS))
                    {
                        strIDS = strIDS.Substring(0, strIDS.Length - 1);

                        cmd.CommandText = @"delete from [dbo].[AttributeSetting] where AttributeSettingID in (" + strIDS + ")";
                        // da.Update(dt);
                        cmd.ExecuteNonQuery();
                    }

                    cmd.CommandText = @"select *,SchemaName from [dbo].[MatchAttributeSetting] where EntitySettingId = '" + objPAM2EntitySetting.EntitySettingId + "'";
                    da = new SqlDataAdapter(cmd);
                    dt = new DataTable();

                    da.Fill(dt);
                    strIDS = String.Empty;
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (!Fields.Contains(Convert.ToString(dt.Rows[i]["SchemaName"])))
                        {
                          //  dt.Rows.RemoveAt(i);
                          //  i--;
                            strIDS += "'" + Convert.ToString(dt.Rows[i]["MatchAttributeSettingId"]) + "',";
                        }
                    }

                  //  da.Update(dt);

                    if (!String.IsNullOrEmpty(strIDS))
                    {
                        strIDS = strIDS.Substring(0, strIDS.Length - 1);

                        cmd.CommandText = @"delete from [dbo].[MatchAttributeSetting] where MatchAttributeSettingId in (" + strIDS + ")";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
        }

        #region Entity Match Group

        public MatchGroup AddEntityMatchGroup(PAMMatchGroupAttributeSetting matchGroupDetails, Guid pamUserId)
        {
            MatchGroup addedMatchGroup = null;
            MatchGroup foundMatchGroup = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (matchGroupDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(matchGroupDetails.EntitySettingId) && !string.IsNullOrEmpty(matchGroupDetails.MatchGroupId))
                {
                    Guid entitySettingIdGUID = new Guid(matchGroupDetails.EntitySettingId);
                    Guid matchGroupIdGuid = new Guid(matchGroupDetails.MatchGroupId);
                    foundMatchGroup = pam2EntitiesContext.MatchGroups.Where(matchGroup => matchGroup.EntitySettingId == entitySettingIdGUID && matchGroup.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
                }
                if (foundMatchGroup != null)
                {
                    foundMatchGroup.DisplayName = matchGroupDetails.GroupName.Trim();
                    foundMatchGroup.IsMaster = true;
                    foundMatchGroup.UpdateDate = DateTime.UtcNow;
                    int DispalyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(matchGroupDetails.DisplayOrder))
                    {
                        Int32.TryParse(matchGroupDetails.DisplayOrder, out DispalyOrder);
                    }
                    foundMatchGroup.DisplayOrder = DispalyOrder;
                    // GroupRule
                    foundMatchGroup.ExcludeFromMasterKey = matchGroupDetails.ExcludeFromMasterKey;
                    foundMatchGroup.PriorityId = new Guid(matchGroupDetails.PriorityId);
                    foundMatchGroup.MatchKeyID = new Guid(matchGroupDetails.MatchKeyID);
                    // GroupRule
                    //this need to be added after integration with CRM
                    foundMatchGroup.UpdatedBy = pamUserId;
                    pam2EntitiesContext.SaveChanges();
                    addedMatchGroup = foundMatchGroup;
                }
                else
                {
                    MatchGroup newMatchGroup = new MatchGroup();
                    newMatchGroup.DisplayName = matchGroupDetails.GroupName.Trim();
                    newMatchGroup.EntitySettingId = new Guid(matchGroupDetails.EntitySettingId);
                    newMatchGroup.IsMaster = true;
                    int DispalyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(matchGroupDetails.DisplayOrder))
                    {
                        Int32.TryParse(matchGroupDetails.DisplayOrder, out DispalyOrder);
                    }
                    newMatchGroup.DisplayOrder = DispalyOrder;
                    //Match Group
                    newMatchGroup.ExcludeFromMasterKey = matchGroupDetails.ExcludeFromMasterKey;
                    newMatchGroup.PriorityId = new Guid(matchGroupDetails.PriorityId);
                    newMatchGroup.MatchKeyID = new Guid(matchGroupDetails.MatchKeyID);
                    //Match Group
                    //this field need to be taken from extjs and will be sent across request and applied
                    newMatchGroup.CreatedBy = pamUserId;
                    newMatchGroup.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.MatchGroups.Add(newMatchGroup);
                    pam2EntitiesContext.SaveChanges();
                    addedMatchGroup = newMatchGroup;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedMatchGroup;
        }

        public ResultSet DeleteEntityMatchGroup(string MatchGroupId)
        {
            ResultSet matchGroupResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchGroup matchGroup = null;
                Guid matchGroupIdGuid = new Guid(MatchGroupId);
                matchGroup = pam2EntitiesContext.MatchGroups.Where(s => s.MatchGroupId == matchGroupIdGuid && s.IsMaster == true).FirstOrDefault();
                if (matchGroup != null)
                {
                    matchGroup = pam2EntitiesContext.MatchGroups.Remove(matchGroup);
                    int count = pam2EntitiesContext.SaveChanges();
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupResult;
        }

        #endregion

        #region Session Match Group

        public MatchGroup GetMatchGroupByID(string MatchGroupID)
        {
            MatchGroup objMatchGroup=null;
            try 
            {   
		        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid matchGroupIdGuid = new Guid(MatchGroupID);
                objMatchGroup = pam2EntitiesContext.MatchGroups.Where(c => c.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
            }
	        catch (Exception ex)
            {
		        throw ex;
	        }
            return objMatchGroup;
        }

        public MatchGroup AddSessionMatchGroup(SessionMatchGroupAttributeSetting matchGroupDetails, Guid pamUserId)
        {
            MatchGroup addedMatchGroup = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchGroup newMatchGroup = new MatchGroup();
                newMatchGroup.DisplayName = matchGroupDetails.GroupName.Trim();
                newMatchGroup.EntitySettingId = new Guid(matchGroupDetails.EntitySettingId);
                newMatchGroup.IsMaster = false;
                int DispalyOrder = 0;
                if (!string.IsNullOrWhiteSpace(matchGroupDetails.DisplayOrder))
                {
                    Int32.TryParse(matchGroupDetails.DisplayOrder, out DispalyOrder);
                }
                newMatchGroup.DisplayOrder = DispalyOrder;
                //this field need to be taken from extjs and will be sent across request and applied
                newMatchGroup.CreatedBy = pamUserId;
                newMatchGroup.CreatedDate = DateTime.UtcNow;
                pam2EntitiesContext.MatchGroups.Add(newMatchGroup);
                pam2EntitiesContext.SaveChanges();
                addedMatchGroup = newMatchGroup;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedMatchGroup;
        }

        public MatchGroup AddUpdateSessionMatchGroup(SessionMatchGroupAttributeSetting sessionMatchGroupDetails, Guid pamUserId)
        {
            MatchGroup addedSessionMatchGroup = null;
            MatchGroup foundSessionMatchGroup = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (!string.IsNullOrEmpty(sessionMatchGroupDetails.MatchGroupId))
                {
                    Guid matchGroupIdGuid = new Guid(sessionMatchGroupDetails.MatchGroupId);

                    foundSessionMatchGroup = pam2EntitiesContext.MatchGroups.Where(sessionGroup => sessionGroup.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
                }
                if (foundSessionMatchGroup != null)
                {
                    foundSessionMatchGroup.UpdateDate = DateTime.UtcNow;
                    foundSessionMatchGroup.EntitySettingId = new Guid(sessionMatchGroupDetails.EntitySettingId);
                    foundSessionMatchGroup.IsMaster = false;
                    int DispalyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sessionMatchGroupDetails.DisplayOrder))
                    {
                        Int32.TryParse(sessionMatchGroupDetails.DisplayOrder, out DispalyOrder);
                    }
                    foundSessionMatchGroup.DisplayOrder = DispalyOrder;
                    foundSessionMatchGroup.DisplayName = sessionMatchGroupDetails.DisplayName;
                    //this need to be added after integration with CRM
                    foundSessionMatchGroup.UpdatedBy = pamUserId;
                    pam2EntitiesContext.SaveChanges();
                    addedSessionMatchGroup = foundSessionMatchGroup;
                }
                else
                {
                    MatchGroup newSessionMatchGroup = new MatchGroup();
                    newSessionMatchGroup.CreatedBy = pamUserId;
                    newSessionMatchGroup.CreatedDate = DateTime.UtcNow;
                    newSessionMatchGroup.DisplayName = sessionMatchGroupDetails.DisplayName;
                    Int32 DisplayOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sessionMatchGroupDetails.DisplayOrder))
                    {
                        Int32.TryParse(sessionMatchGroupDetails.DisplayOrder, out DisplayOrder);
                    }

                    newSessionMatchGroup.DisplayOrder = DisplayOrder;
                    newSessionMatchGroup.EntitySettingId = new Guid(sessionMatchGroupDetails.EntitySettingId);
                    newSessionMatchGroup.IsMaster = false;
                    pam2EntitiesContext.MatchGroups.Add(newSessionMatchGroup);
                    pam2EntitiesContext.SaveChanges();
                    addedSessionMatchGroup = newSessionMatchGroup;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSessionMatchGroup;
        }

         

        public ResultSet DeleteSessionMatchGroup(string MatchGroupId)
        {
            ResultSet matchGroupResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchGroup matchGroup = null;
                Guid matchGroupIdGuid = new Guid(MatchGroupId);
                matchGroup = pam2EntitiesContext.MatchGroups.Where(s => s.MatchGroupId == matchGroupIdGuid && s.IsMaster == false).FirstOrDefault();
                if (matchGroup != null)
                {
                    matchGroup = pam2EntitiesContext.MatchGroups.Remove(matchGroup);
                    int count = pam2EntitiesContext.SaveChanges();
                }
                matchGroupResult.Message = "Success";
                matchGroupResult.Result = true;
            }
            catch (Exception excObj)
            {
                matchGroupResult.Message = excObj.ToString();
                matchGroupResult.Result = false;
                throw excObj;
            }
            return matchGroupResult;
        }

        public bool CheckMatchGroupInSessionGroup(string MatchGroupID, string SessionID)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                SessionGroup matchGroup = null;
                Guid matchGroupIdGuid = new Guid(MatchGroupID);
                Guid SessionIDGuid = new Guid(SessionID);

                matchGroup = pam2EntitiesContext.SessionGroups.Where(s => s.MatchGroupId == matchGroupIdGuid && s.SessionId == SessionIDGuid).FirstOrDefault();
                if (matchGroup == null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return true;
        }

        #endregion

        #region Session Group

        public SessionGroup AddUpdateSessionGroup(SessionMatchGroupAttributeSetting sessionGroupDetails, Guid pamUserId)
        {
            SessionGroup addedSessionGroup = null;
            SessionGroup foundSessionGroup = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (sessionGroupDetails.SessionId != null && !string.IsNullOrWhiteSpace(sessionGroupDetails.SessionId) && !string.IsNullOrEmpty(sessionGroupDetails.MatchGroupId))
                {
                    Guid matchGroupIdGuid = new Guid(sessionGroupDetails.MatchGroupId);
                    Guid sessionIdGuid = new Guid(sessionGroupDetails.SessionId);
                    foundSessionGroup = pam2EntitiesContext.SessionGroups.Where(sessionGroup => sessionGroup.SessionId == sessionIdGuid && sessionGroup.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
                }
                if (foundSessionGroup != null)
                {
                    foundSessionGroup.ExcludeFromMasterKey = sessionGroupDetails.ExcludeFromMasterKey;
                    foundSessionGroup.MatchKeyID = new Guid(sessionGroupDetails.MatchKeyID);
                    foundSessionGroup.MatchGroupId = new Guid(sessionGroupDetails.MatchGroupId);
                    foundSessionGroup.UpdateDate = DateTime.UtcNow;
                    if (!string.IsNullOrWhiteSpace(sessionGroupDetails.PriorityId))
                        foundSessionGroup.PriorityId = new Guid(sessionGroupDetails.PriorityId);
                    foundSessionGroup.SessionId = new Guid(sessionGroupDetails.SessionId);
                    //this need to be added after integration with CRM
                    foundSessionGroup.UpdatedBy = pamUserId;
                    pam2EntitiesContext.SaveChanges();
                    addedSessionGroup = foundSessionGroup;
                }
                else
                {
                    SessionGroup newSessionGroup = new SessionGroup();
                    newSessionGroup.ExcludeFromMasterKey = sessionGroupDetails.ExcludeFromMasterKey;
                    newSessionGroup.MatchKeyID = new Guid(sessionGroupDetails.MatchKeyID);
                    newSessionGroup.MatchGroupId = new Guid(sessionGroupDetails.MatchGroupId);
                    if (!string.IsNullOrWhiteSpace(sessionGroupDetails.PriorityId))
                        newSessionGroup.PriorityId = new Guid(sessionGroupDetails.PriorityId);
                    newSessionGroup.SessionId = new Guid(sessionGroupDetails.SessionId);
                    //this field need to be taken from extjs and will be sent across request and applied
                    newSessionGroup.CreatedBy = pamUserId;
                    newSessionGroup.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SessionGroups.Add(newSessionGroup);
                    pam2EntitiesContext.SaveChanges();
                    addedSessionGroup = newSessionGroup;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSessionGroup;
        }

        public ResultSet DeleteSessionGroup(string MatchGroupId, string sessionId)
        {
            ResultSet matchGroupResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                SessionGroup sessionGroup = null;
                Guid matchGroupIdGuid = new Guid(MatchGroupId);
                Guid sessionIdGuid = new Guid(sessionId);
                sessionGroup = pam2EntitiesContext.SessionGroups.Where(s => s.MatchGroupId == matchGroupIdGuid && s.SessionId == sessionIdGuid).FirstOrDefault();
                if (sessionGroup != null)
                {
                    sessionGroup = pam2EntitiesContext.SessionGroups.Remove(sessionGroup);
                    int count = pam2EntitiesContext.SaveChanges();
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupResult;
        }

        #endregion

        #region Match Group Attribute Setting

        public MatchGroupAttributeSettingResultSet GetMatchGroupAttributeSettings(string entitySettingId)
        {
            var matchGroupAttributeSettingResultSet = new MatchGroupAttributeSettingResultSet();
            Guid entitySettingIdGUID = new Guid(entitySettingId);
            var lstMatchGroupAttributeSetting = new List<PAMMatchGroupAttributeSetting>();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                var lstResult = (from matchGroup in pam2EntitiesContext.MatchGroups
                                 join matchGroupAttributeSetting in pam2EntitiesContext.MatchAttributeSettings.OrderBy(c => c.DisplayOrder) on matchGroup.MatchGroupId equals matchGroupAttributeSetting.MatchGroupId
                                 into result
                                 //orderby matchGroup.MatchGroupId ascending
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby matchGroup.DisplayOrder, index
                                 where matchGroup.EntitySettingId == entitySettingIdGUID && matchGroup.IsMaster == true
                                 from finalResult in result.DefaultIfEmpty()
                                 select new
                                 {
                                     GroupName = matchGroup.DisplayName,
                                     EntitySettingId = matchGroup.EntitySettingId,
                                     IsMatchGroupMaster = matchGroup.IsMaster,
                                     MatchGroupId = matchGroup.MatchGroupId,
                                     // for Group Rule
                                     PriorityId = matchGroup.PriorityId,
                                     MatchKeyId = matchGroup.MatchKeyID,
                                     ExcludeMasterKey = matchGroup.ExcludeFromMasterKey,
                                     //Group Rule
                                     DisplayName = (finalResult == null ? string.Empty : finalResult.DisplayName),
                                     SchemaName = (finalResult == null ? string.Empty : finalResult.SchemaName),
                                     MatchAttributeSettingId = (finalResult == null ? Guid.Empty : finalResult.MatchAttributeSettingId),
                                     MatchGroupDisplayOrder = matchGroup.DisplayOrder,
                                     AttributeDisplayOrder = (finalResult == null ? 0 : finalResult.DisplayOrder)
                                 }
                                 ).Distinct().OrderBy(c => c.MatchGroupDisplayOrder).ThenBy(x => x.AttributeDisplayOrder).ToList();

                var parentNode = new PAMMatchGroupAttributeSetting();
                Guid previousMatchGroupId = Guid.Empty;
                var lstChildren = new List<PAMMatchGroupAttributeSetting>();

                foreach (var obj in lstResult)
                {
                    if (obj == null)
                        continue;
                    var childNode = new PAMMatchGroupAttributeSetting();
                    if (previousMatchGroupId == Guid.Empty || (obj.MatchGroupId != null && previousMatchGroupId != obj.MatchGroupId))
                    {

                        parentNode = new PAMMatchGroupAttributeSetting();
                        parentNode.children = new List<PAMMatchGroupAttributeSetting>();

                        lstChildren = new List<PAMMatchGroupAttributeSetting>();
                        parentNode.leaf = false;
                        parentNode.text = obj.GroupName;
                        parentNode.cls = "folder";
                        parentNode.EntitySettingId = obj.EntitySettingId.ToString();
                        parentNode.GroupName = obj.GroupName;
                        parentNode.id = obj.MatchGroupId.ToString();
                        //parentCount += 1;
                        //parentNode.MatchAttributeSettingId = parentCount.ToString();
                        parentNode.DisplayOrder = Convert.ToString(obj.AttributeDisplayOrder);
                        parentNode.MatchGroupId = obj.MatchGroupId.ToString();
                        parentNode.qtip = obj.GroupName;
                        parentNode.DisplayName = obj.GroupName;
                        parentNode.SchemaName = "";
                        // for Group Rule
                        parentNode.PriorityId = obj.PriorityId.ToString();
                        parentNode.MatchKeyID = obj.MatchKeyId.ToString();
                        parentNode.ExcludeFromMasterKey = (bool)obj.ExcludeMasterKey;
                        //Group Rule
                        lstMatchGroupAttributeSetting.Add(parentNode);
                    }

                    if (obj.MatchAttributeSettingId != null && obj.MatchAttributeSettingId != Guid.Empty)
                    {
                        childNode.leaf = true;
                        childNode.text = obj.DisplayName;
                        childNode.cls = "file";
                        childNode.EntitySettingId = obj.EntitySettingId.ToString();
                        childNode.GroupName = obj.GroupName;
                        childNode.id = obj.MatchAttributeSettingId.ToString();
                        childNode.MatchAttributeSettingId = obj.MatchAttributeSettingId.ToString();
                        childNode.DisplayOrder = Convert.ToString(obj.AttributeDisplayOrder);
                        childNode.MatchGroupId = obj.MatchGroupId.ToString();
                        childNode.qtip = obj.DisplayName;
                        childNode.SchemaName = obj.SchemaName;
                        childNode.DisplayName = obj.DisplayName;
                        lstChildren.Add(childNode);
                        parentNode.children = lstChildren;
                    }
                    previousMatchGroupId = obj.MatchGroupId;
                }
                //lstMatchGroupAttributeSetting.Add(parentNode);
                matchGroupAttributeSettingResultSet.Message = "Success";
                matchGroupAttributeSettingResultSet.MatchGroupAttributeSettings = lstMatchGroupAttributeSetting;
                matchGroupAttributeSettingResultSet.Result = true;
                matchGroupAttributeSettingResultSet.success = true;
                matchGroupAttributeSettingResultSet.total = lstMatchGroupAttributeSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupAttributeSettingResultSet;
        }

        public MatchAttributeSetting AddEntityMatchGroupAttributeSetting(PAMMatchGroupAttributeSetting matchGroupAttributeDetails, Guid pamUserId)
        {
            MatchAttributeSetting addedMatchGroupAttributeSetting = null;
            MatchAttributeSetting foundMatchGroupAttributeSetting = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (matchGroupAttributeDetails.MatchAttributeSettingId != null && !string.IsNullOrWhiteSpace(matchGroupAttributeDetails.MatchAttributeSettingId) && matchGroupAttributeDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(matchGroupAttributeDetails.EntitySettingId) && !string.IsNullOrEmpty(matchGroupAttributeDetails.MatchGroupId))
                {
                    Guid entitySettingIdGUID = new Guid(matchGroupAttributeDetails.EntitySettingId);
                    Guid matchGroupIdGuid = new Guid(matchGroupAttributeDetails.MatchGroupId);
                    Guid matchGroupAttributeSettingIdGuid = new Guid(matchGroupAttributeDetails.MatchAttributeSettingId);
                    foundMatchGroupAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.Where(matchGroupAttributeSetting => matchGroupAttributeSetting.MatchAttributeSettingId == matchGroupAttributeSettingIdGuid && matchGroupAttributeSetting.EntitySettingId == entitySettingIdGUID && matchGroupAttributeSetting.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
                }
                if (foundMatchGroupAttributeSetting != null)
                {
                    foundMatchGroupAttributeSetting.DisplayName = matchGroupAttributeDetails.DisplayName;
                    foundMatchGroupAttributeSetting.UpdateDate = DateTime.UtcNow;
                    // this needs to be implemented when integrated with CRM
                    int DispalyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(matchGroupAttributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(matchGroupAttributeDetails.DisplayOrder, out DispalyOrder);
                    }

                    foundMatchGroupAttributeSetting.DisplayOrder = DispalyOrder;
                    foundMatchGroupAttributeSetting.UpdatedBy = pamUserId;
                    foundMatchGroupAttributeSetting.EntitySettingId = new Guid(matchGroupAttributeDetails.EntitySettingId);
                    foundMatchGroupAttributeSetting.SchemaName = matchGroupAttributeDetails.SchemaName;
                    foundMatchGroupAttributeSetting.SessionId = null;
                    // for Group Rule
                    foundMatchGroupAttributeSetting.MatchKey = matchGroupAttributeDetails.MatchKeyID;
                    foundMatchGroupAttributeSetting.ExcludeFromMasterKey = matchGroupAttributeDetails.ExcludeFromMasterKey;
                    foundMatchGroupAttributeSetting.Priority = matchGroupAttributeDetails.PriorityId;
                    //Group Rule
                    pam2EntitiesContext.SaveChanges();
                    addedMatchGroupAttributeSetting = foundMatchGroupAttributeSetting;
                }
                else
                {
                    MatchAttributeSetting newMatchGroupAttributeSetting = new MatchAttributeSetting();

                    newMatchGroupAttributeSetting.DisplayName = matchGroupAttributeDetails.DisplayName;
                    newMatchGroupAttributeSetting.EntitySettingId = new Guid(matchGroupAttributeDetails.EntitySettingId);
                    newMatchGroupAttributeSetting.MatchGroupId = new Guid(matchGroupAttributeDetails.MatchGroupId);
                    newMatchGroupAttributeSetting.SchemaName = matchGroupAttributeDetails.SchemaName;
                    newMatchGroupAttributeSetting.SessionId = null;

                    int DispalyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(matchGroupAttributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(matchGroupAttributeDetails.DisplayOrder, out DispalyOrder);
                    }
                    newMatchGroupAttributeSetting.DisplayOrder = DispalyOrder;
                    // for Group Rule
                    newMatchGroupAttributeSetting.MatchKey = matchGroupAttributeDetails.MatchKeyID;
                    newMatchGroupAttributeSetting.ExcludeFromMasterKey = matchGroupAttributeDetails.ExcludeFromMasterKey;
                    newMatchGroupAttributeSetting.Priority = matchGroupAttributeDetails.PriorityId;
                    //Group Rule

                    //this field need to be taken from extjs and will be sent across request and applied
                    newMatchGroupAttributeSetting.CreatedBy = pamUserId;
                    newMatchGroupAttributeSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.MatchAttributeSettings.Add(newMatchGroupAttributeSetting);
                    pam2EntitiesContext.SaveChanges();
                    addedMatchGroupAttributeSetting = newMatchGroupAttributeSetting;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedMatchGroupAttributeSetting;
        }

        public ResultSet DeleteEntityMatchGroupAttributeSetting(string MatchGroupAttributeSettingId)
        {
            var matchGroupResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchAttributeSetting matchGroupAttributeSetting = null;
                Guid matchGroupAttributeSettingIdGuid = new Guid(MatchGroupAttributeSettingId);
                matchGroupAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.Where(s => s.MatchAttributeSettingId == matchGroupAttributeSettingIdGuid).FirstOrDefault();
                if (matchGroupAttributeSetting != null)
                {
                    matchGroupAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.Remove(matchGroupAttributeSetting);
                    int count = pam2EntitiesContext.SaveChanges();
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupResult;
        }

        #endregion

        #region Session MatchGroup AttributeSetting

        public SessionMatchGroupAttributeSettingResultSet GetSessionMatchGroupAttributeSettings(string entitySettingId, string sessionId)
        {
            var sessionMatchGroupAttributeSettingResultSet = new SessionMatchGroupAttributeSettingResultSet();
            var lstSessionMatchGroupAttributeSetting = new List<SessionMatchGroupAttributeSetting>();
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    sessionMatchGroupAttributeSettingResultSet = GetDefaultSessionMatchGroupAttributeSettings(entitySettingId);
                    return sessionMatchGroupAttributeSettingResultSet;
                }
                Guid entitySettingIdGUID = new Guid(entitySettingId);
                Guid sessionIdGuid = new Guid(sessionId);

                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                var lstResult = (from sessionGroup in pam2EntitiesContext.SessionGroups
                                 join matchGroup in pam2EntitiesContext.MatchGroups on sessionGroup.MatchGroupId equals matchGroup.MatchGroupId
                                 join matchGroupAttributeSetting in pam2EntitiesContext.MatchAttributeSettings on matchGroup.MatchGroupId equals matchGroupAttributeSetting.MatchGroupId
                                 into result
                                 //orderby matchGroup.MatchGroupId ascending
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby matchGroup.DisplayOrder, index
                                 where sessionGroup.SessionId == sessionIdGuid && matchGroup.EntitySettingId == entitySettingIdGUID
                                 from finalResult in result.DefaultIfEmpty()
                                 select new
                                 {
                                     SessionGroupObj = sessionGroup,
                                     MatchGroupObj = matchGroup,
                                     MatchgroupAttributeSettingId = finalResult.MatchAttributeSettingId != null ? finalResult.MatchAttributeSettingId : Guid.Empty,
                                     SchemaName = finalResult.SchemaName,
                                     AttributeDisplayName = finalResult.DisplayName,
                                     AttributeDisplayOrder = (finalResult == null ? 0 : finalResult.DisplayOrder),
                                     MatchGroupDisplayOrder = matchGroup.DisplayOrder
                                 }
                                ).Distinct().OrderBy(c => c.MatchGroupDisplayOrder).ThenBy(x => x.AttributeDisplayOrder).ToList();

                var parentNode = new SessionMatchGroupAttributeSetting();
                Guid previousMatchGroupId = Guid.Empty;
                var lstChildren = new List<SessionMatchGroupAttributeSetting>();
                if (lstResult != null && lstResult.Count > 0)
                {
                    foreach (var obj in lstResult)
                    {
                        if (obj == null)
                            continue;
                        var childNode = new SessionMatchGroupAttributeSetting();
                        if (previousMatchGroupId == Guid.Empty || (obj.MatchGroupObj.MatchGroupId != null && previousMatchGroupId != obj.MatchGroupObj.MatchGroupId))
                        {
                            parentNode = new SessionMatchGroupAttributeSetting();
                            parentNode.children = new List<SessionMatchGroupAttributeSetting>();
                            lstChildren = new List<SessionMatchGroupAttributeSetting>();
                            parentNode.ExcludeFromMasterKey = obj.SessionGroupObj.ExcludeFromMasterKey != null ? Convert.ToBoolean(obj.SessionGroupObj.ExcludeFromMasterKey) : false;
                            parentNode.MatchKeyID = Convert.ToString(obj.SessionGroupObj.MatchKeyID);
                            parentNode.PriorityId = obj.SessionGroupObj.PriorityId.ToString();
                            parentNode.SessionId = sessionId;
                            parentNode.leaf = false;
                            parentNode.text = obj.MatchGroupObj.DisplayName;
                            parentNode.cls = "folder";
                            parentNode.EntitySettingId = obj.MatchGroupObj.EntitySettingId.ToString();
                            parentNode.GroupName = obj.MatchGroupObj.DisplayName;
                            parentNode.id = obj.MatchGroupObj.MatchGroupId.ToString();
                            //parentCount += 1;
                            //parentNode.MatchAttributeSettingId = parentCount.ToString();
                            parentNode.MatchGroupId = obj.MatchGroupObj.MatchGroupId.ToString();
                            parentNode.qtip = obj.MatchGroupObj.DisplayName;
                            parentNode.DisplayName = obj.MatchGroupObj.DisplayName;
                            parentNode.SchemaName = "";
                            parentNode.DisplayOrder = Convert.ToString(obj.MatchGroupDisplayOrder);
                            lstSessionMatchGroupAttributeSetting.Add(parentNode);
                        }

                        if (obj.MatchgroupAttributeSettingId != null && obj.MatchgroupAttributeSettingId != Guid.Empty)
                        {
                            childNode.leaf = true;
                            childNode.text = obj.AttributeDisplayName;
                            childNode.cls = "file";
                            childNode.EntitySettingId = obj.MatchGroupObj.EntitySettingId.ToString();
                            childNode.GroupName = obj.MatchGroupObj.DisplayName;
                            childNode.id = obj.MatchgroupAttributeSettingId.ToString();
                            childNode.MatchAttributeSettingId = obj.MatchgroupAttributeSettingId.ToString();
                            childNode.MatchGroupId = obj.MatchGroupObj.MatchGroupId.ToString();
                            childNode.qtip = obj.AttributeDisplayName;
                            childNode.SchemaName = obj.SchemaName;
                            childNode.DisplayName = obj.AttributeDisplayName;
                            // childNode.ExcludeFromMasterKey = obj.SessionGroupObj.ExcludeFromMasterKey != null ? Convert.ToBoolean(obj.SessionGroupObj.ExcludeFromMasterKey) : false;
                            // childNode.PriorityId = obj.SessionGroupObj.PriorityId.ToString();
                            childNode.SessionId = sessionId;
                            childNode.DisplayOrder = Convert.ToString(obj.AttributeDisplayOrder);
                            lstChildren.Add(childNode);
                            parentNode.children = lstChildren;
                        }

                        previousMatchGroupId = obj.MatchGroupObj.MatchGroupId;
                    }
                    sessionMatchGroupAttributeSettingResultSet.Message = "Success";
                    sessionMatchGroupAttributeSettingResultSet.SessionMatchGroupAttributeSettings = lstSessionMatchGroupAttributeSetting;//.OrderBy(p => p.text).ToList<SessionMatchGroupAttributeSetting>();
                    sessionMatchGroupAttributeSettingResultSet.Result = true;
                    sessionMatchGroupAttributeSettingResultSet.success = true;
                    sessionMatchGroupAttributeSettingResultSet.total = lstSessionMatchGroupAttributeSetting.Count;
                }
                else
                {
                    //  sessionMatchGroupAttributeSettingResultSet = GetDefaultSessionMatchGroupAttributeSettings(entitySettingId);
                    //  return sessionMatchGroupAttributeSettingResultSet;

                    sessionMatchGroupAttributeSettingResultSet.Message = "Success";
                    sessionMatchGroupAttributeSettingResultSet.SessionMatchGroupAttributeSettings = lstSessionMatchGroupAttributeSetting;//.OrderBy(p => p.text).ToList<SessionMatchGroupAttributeSetting>();
                    sessionMatchGroupAttributeSettingResultSet.Result = true;
                    sessionMatchGroupAttributeSettingResultSet.success = true;
                    sessionMatchGroupAttributeSettingResultSet.total = lstSessionMatchGroupAttributeSetting.Count;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sessionMatchGroupAttributeSettingResultSet;
        }

        private SessionMatchGroupAttributeSettingResultSet GetDefaultSessionMatchGroupAttributeSettings(string entitySettingId)
        {
            var sessionMatchGroupAttributeSettingResultSet = new SessionMatchGroupAttributeSettingResultSet();
            var lstSessionMatchGroupAttributeSetting = new List<SessionMatchGroupAttributeSetting>();

            try
            {
                Guid entitySettingIdGUID = new Guid(entitySettingId);
                var parentNode = new SessionMatchGroupAttributeSetting();
                Guid previousMatchGroupId = Guid.Empty;
                var lstChildren = new List<SessionMatchGroupAttributeSetting>();
                var matchGroupSetting = GetMatchGroupAttributeSettings(entitySettingId);
                if (matchGroupSetting != null && matchGroupSetting.MatchGroupAttributeSettings != null && matchGroupSetting.MatchGroupAttributeSettings.Count > 0)
                {
                    int matchGroupId = 1;
                    foreach (var obj in matchGroupSetting.MatchGroupAttributeSettings)
                    {
                        if (obj == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(obj.MatchGroupId))
                            continue;

                        var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                        PriorityMaster objPriorityMaster = pam2EntitiesContext.PriorityMasters.Where(c => c.Name.ToLower() == "normal").FirstOrDefault();
                        MatchKeyMaster objMatchKeyMaster = pam2EntitiesContext.MatchKeyMasters.Where(c => c.MatchKey.ToLower() == "dqfonetix").FirstOrDefault();

                        if (previousMatchGroupId == Guid.Empty || (obj.MatchGroupId != null && !string.IsNullOrWhiteSpace(obj.MatchGroupId) && previousMatchGroupId.ToString() != obj.MatchGroupId))
                        {
                            parentNode = new SessionMatchGroupAttributeSetting();
                            parentNode.children = new List<SessionMatchGroupAttributeSetting>();
                            lstChildren = new List<SessionMatchGroupAttributeSetting>();
                            parentNode.ExcludeFromMasterKey = obj.ExcludeFromMasterKey;// false;
                            parentNode.PriorityId = Convert.ToString(obj.PriorityId); //Convert.ToString(objPriorityMaster.PriorityId);
                            parentNode.leaf = false;
                            parentNode.text = obj.GroupName;
                            parentNode.cls = "folder";
                            parentNode.EntitySettingId = obj.EntitySettingId;
                            parentNode.GroupName = obj.GroupName;
                            parentNode.id = (matchGroupId++).ToString();//obj.MatchGroupId;
                            parentNode.MatchGroupId = obj.MatchGroupId;
                            parentNode.MatchKeyID = Convert.ToString(obj.MatchKeyID);// Convert.ToString(objMatchKeyMaster.MatchKeyID);
                            parentNode.qtip = obj.GroupName;
                            parentNode.DisplayName = obj.GroupName;
                            parentNode.SchemaName = "";

                            lstSessionMatchGroupAttributeSetting.Add(parentNode);
                        }

                        if (obj.children != null && obj.children.Count > 0)
                        {
                            foreach (var item in obj.children)
                            {
                                var childNode = new SessionMatchGroupAttributeSetting();
                                childNode.leaf = true;
                                childNode.text = item.DisplayName;
                                childNode.cls = "file";
                                childNode.EntitySettingId = item.EntitySettingId;
                                childNode.GroupName = item.GroupName;
                                childNode.id = item.MatchAttributeSettingId;
                                //childNode.MatchAttributeSettingId = item.MatchAttributeSettingId;
                                childNode.MatchGroupId = item.MatchGroupId;
                                childNode.qtip = item.DisplayName;
                                childNode.SchemaName = item.SchemaName;
                                childNode.DisplayName = item.DisplayName;
                                lstChildren.Add(childNode);
                                parentNode.children = lstChildren;
                            }
                        }
                        previousMatchGroupId = new Guid(obj.MatchGroupId);
                    }
                }
                sessionMatchGroupAttributeSettingResultSet.Message = "Success";
                sessionMatchGroupAttributeSettingResultSet.SessionMatchGroupAttributeSettings = lstSessionMatchGroupAttributeSetting;
                sessionMatchGroupAttributeSettingResultSet.Result = true;
                sessionMatchGroupAttributeSettingResultSet.success = true;
                sessionMatchGroupAttributeSettingResultSet.total = lstSessionMatchGroupAttributeSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sessionMatchGroupAttributeSettingResultSet;
        }

        public MatchAttributeSetting AddSessionMatchGroupAttributeSetting(SessionMatchGroupAttributeSetting sessionMatchGroupAttributeDetails, Guid pamUserId, string sessionId)
        {
            MatchAttributeSetting addedMatchGroupAttributeSetting = null;
            MatchAttributeSetting foundMatchGroupAttributeSetting = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (!string.IsNullOrWhiteSpace(sessionId) && sessionMatchGroupAttributeDetails.MatchAttributeSettingId != null && !string.IsNullOrWhiteSpace(sessionMatchGroupAttributeDetails.MatchAttributeSettingId) && sessionMatchGroupAttributeDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(sessionMatchGroupAttributeDetails.EntitySettingId) && !string.IsNullOrEmpty(sessionMatchGroupAttributeDetails.MatchGroupId))
                {
                    Guid entitySettingIdGUID = new Guid(sessionMatchGroupAttributeDetails.EntitySettingId);
                    Guid matchGroupIdGuid = new Guid(sessionMatchGroupAttributeDetails.MatchGroupId);
                    Guid matchGroupAttributeSettingIdGuid = new Guid(sessionMatchGroupAttributeDetails.MatchAttributeSettingId);
                    Guid sessionIdGuid = new Guid(sessionId);
                    //  foundMatchGroupAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.Where(matchGroupAttributeSetting => matchGroupAttributeSetting.SessionId == sessionIdGuid && matchGroupAttributeSetting.MatchAttributeSettingId == matchGroupAttributeSettingIdGuid && matchGroupAttributeSetting.EntitySettingId == entitySettingIdGUID && matchGroupAttributeSetting.MatchGroupId == matchGroupIdGuid).FirstOrDefault();
                }
                /*  if (foundMatchGroupAttributeSetting != null)
                  {
                      foundMatchGroupAttributeSetting.DisplayName = sessionMatchGroupAttributeDetails.DisplayName;
                      foundMatchGroupAttributeSetting.UpdateDate = DateTime.Now;
                      // this needs to be implemented when integrated with CRM
                      foundMatchGroupAttributeSetting.UpdatedBy = pamUserId;
                      foundMatchGroupAttributeSetting.EntitySettingId = new Guid(sessionMatchGroupAttributeDetails.EntitySettingId);
                      foundMatchGroupAttributeSetting.SchemaName = sessionMatchGroupAttributeDetails.SchemaName;
                      foundMatchGroupAttributeSetting.SessionId = new Guid(sessionId);
                      pam2EntitiesContext.SaveChanges();
                      addedMatchGroupAttributeSetting = foundMatchGroupAttributeSetting;
                  }
                  else*/
                {
                    MatchAttributeSetting newMatchGroupAttributeSetting = new MatchAttributeSetting();
                    newMatchGroupAttributeSetting.DisplayName = sessionMatchGroupAttributeDetails.DisplayName;
                    newMatchGroupAttributeSetting.EntitySettingId = new Guid(sessionMatchGroupAttributeDetails.EntitySettingId);
                    newMatchGroupAttributeSetting.MatchGroupId = new Guid(sessionMatchGroupAttributeDetails.MatchGroupId);
                    newMatchGroupAttributeSetting.SchemaName = sessionMatchGroupAttributeDetails.SchemaName;
                    newMatchGroupAttributeSetting.SessionId = new Guid(sessionId);
                    int DispalyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sessionMatchGroupAttributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(sessionMatchGroupAttributeDetails.DisplayOrder, out DispalyOrder);
                    }
                    newMatchGroupAttributeSetting.DisplayOrder = DispalyOrder;
                    //this field need to be taken from extjs and will be sent across request and applied
                    newMatchGroupAttributeSetting.CreatedBy = pamUserId;
                    newMatchGroupAttributeSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.MatchAttributeSettings.Add(newMatchGroupAttributeSetting);
                    pam2EntitiesContext.SaveChanges();
                    addedMatchGroupAttributeSetting = newMatchGroupAttributeSetting;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedMatchGroupAttributeSetting;
        }

        public ResultSet DeleteSessionMatchGroupAttributeSetting(string MatchGroupAttributeSettingId, string sessionId)
        {
            var matchGroupResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                MatchAttributeSetting matchGroupAttributeSetting = null;
                Guid matchGroupAttributeSettingIdGuid = new Guid(MatchGroupAttributeSettingId);
                Guid sessionIdGuid = new Guid(sessionId);
                matchGroupAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.Where(s => s.MatchAttributeSettingId == matchGroupAttributeSettingIdGuid).FirstOrDefault(); //&& s.SessionId == sessionIdGuid
                if (matchGroupAttributeSetting != null)
                {
                    matchGroupAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.Remove(matchGroupAttributeSetting);
                    int count = pam2EntitiesContext.SaveChanges();
                    matchGroupResult.Message = "Success";
                    matchGroupResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchGroupResult;
        }

        public ResultSet DeleteAllSessionMatchGroupAttributeSetting(Guid MatchGroupId)
        {
            var matchAttributeSettingResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<MatchAttributeSetting> matchAttributeSettingList = null;
                matchAttributeSettingList = pam2EntitiesContext.MatchAttributeSettings.Where(s => s.MatchGroupId == MatchGroupId).ToList();
                if (matchAttributeSettingList != null && matchAttributeSettingList.Count > 0)
                {
                    foreach (var matchAttribute in matchAttributeSettingList)
                    {
                        pam2EntitiesContext.MatchAttributeSettings.Remove(matchAttribute);
                        int count = pam2EntitiesContext.SaveChanges();
                    }
                }
                matchAttributeSettingResult.Message = "Success";
                matchAttributeSettingResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return matchAttributeSettingResult;
        }

        #endregion

        #region Customer Users

        public UserResultSet GetUsers()
        {
            List<PAMUser> lstUser = new List<PAMUser>();
            UserResultSet userResult = new UserResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                lstUser = (from user in pam2EntitiesContext.Users
                           where user.IsDeleted == false
                           select new
                           {
                               CRMUserId = user.CRMUserId,
                               Email = user.Email,
                               FirstName = user.FirstName,
                               GridHeight = user.GridHeight,
                               GridWidth = user.GridWidth,
                               IsDeleted = user.IsDeleted,
                               IsPrimary = user.IsPrimary,
                               LastName = user.LastName,
                               Password = user.Password,
                               Phone = user.Phone,
                               SkipDeferred = user.SkipDeferred,
                               UserId = user.UserId,
                               UserName = user.UserName
                           }).AsEnumerable().Select(finalUser => new PAMUser
                                {
                                    CRMUserId = finalUser.CRMUserId,
                                    Email = finalUser.Email,
                                    FirstName = finalUser.FirstName,
                                    GridHeight = finalUser.GridHeight,
                                    GridWidth = finalUser.GridWidth,
                                    Active = !finalUser.IsDeleted,
                                    IsPrimary = finalUser.IsPrimary,
                                    LastName = finalUser.LastName,
                                    Password = finalUser.Password,
                                    Phone = finalUser.Phone,
                                    SkipDeferred = finalUser.SkipDeferred,
                                    UserId = finalUser.UserId,
                                    UserName = finalUser.UserName
                                }).ToList<PAMUser>();

                foreach (PAMUser pamUser in lstUser)
                {
                    List<Role> roles = GetUserRoles(pamUser.UserId.ToString());

                    if (roles != null)
                    {
                        for (int i = 0; i < roles.Count; i++)
                        {
                            if (roles[i].Name.ToLower().Equals("administrator"))
                            {
                                pamUser.IsAdministrator = true;
                            }
                            if (roles[i].Name.ToLower().Equals("reviewer"))
                            {
                                pamUser.IsReviewer = true;
                            }
                        }
                    }
                }

                userResult.Message = "Success";
                userResult.success = true;
                userResult.Users = lstUser;
                userResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return userResult;
        }

        private List<Role> GetUserRoles(string userId)
        {
            List<Role> userRoles = null;
            try
            {
                Guid userIdGuid = (new Guid(userId));
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                userRoles = (from role in pam2EntitiesContext.Roles
                             join userRole in pam2EntitiesContext.UserRoles on role.RoleId equals userRole.RoleId
                             where userRole.UserId == userIdGuid
                             select role
                                    ).ToList<Role>();

            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return userRoles;
        }

        public UserResultSet AddPAMUser(string crmUserId, string userName, string password, string firstName, string lastName, string phone, string email, string CreatedBy)
        {
            UserResultSet userResult = new UserResultSet();
            User addedUser = null;
            User newUser = new User();
            try
            {
                
                newUser.CreatedBy = new Guid(CreatedBy);
                newUser.CreatedDate = DateTime.UtcNow;
                newUser.CRMUserId = new Guid(crmUserId);
                newUser.Email = email;
                newUser.FirstName = firstName;
                newUser.GridHeight = Convert.ToInt32(ConfigurationManager.AppSettings["GridHeight"]); // 350;
                newUser.GridWidth = Convert.ToInt32(ConfigurationManager.AppSettings["GridWidth"]); // 250;
                newUser.IsDeleted = false;
                newUser.IsPrimary = false;
                newUser.LastName = lastName;
                newUser.Password = password;
                newUser.Phone = phone;
                newUser.SkipDeferred = false;
                newUser.UserName = userName;

                if (newUser.UserId == Guid.Empty)
                    newUser.UserId = Guid.NewGuid();

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                addedUser = pam2EntitiesContext.Users.Add(newUser);
                pam2EntitiesContext.SaveChanges();

                if (addedUser != null && addedUser.UserId != Guid.Empty)
                {
                    userResult.Users = new List<PAMUser>();
                    userResult.Users.Add(new PAMUser { UserId = addedUser.UserId });
                    userResult.Message = "Success";
                    userResult.success = true;
                    userResult.total = 1;
                    userResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return userResult;
        }

        public ResultSet DeletePAMUser(string userId)
        {
            ResultSet userResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                User user = null;
                Guid userIdGuid = new Guid(userId);
                user = pam2EntitiesContext.Users.Where(s => s.UserId == userIdGuid).FirstOrDefault();
                if (user != null)
                {
                    if ((user.IsPrimary != null) && (bool)(user.IsPrimary))
                    {
                        return userResult;
                    }
                    user.IsDeleted = true;
                    int count = pam2EntitiesContext.SaveChanges();
                    userResult.Message = "Success";
                    userResult.Result = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return userResult;
        }

        public ResultSet UpdatePAMUser(string userId, bool isPrimary, string updatedBy)
        {
            ResultSet userResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                User user = null;
                Guid userIdGuid = new Guid(userId);
                user = pam2EntitiesContext.Users.Where(s => s.UserId == userIdGuid).FirstOrDefault();
                if (user != null)
                {
                    user.IsPrimary = isPrimary;
                    user.UpdateDate = DateTime.UtcNow;
                    user.UpdatedBy = new Guid(updatedBy);
                    int count = pam2EntitiesContext.SaveChanges();
                    userResult.Message = "Success";
                    userResult.Result = true;
                    userResult.success = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return userResult;
        }

        public User CheckValidUserAndGetRole(string CRMUserID)
        {
            User objUser = null;

            try
            {

                TraceLog.Write("Start " + MethodBase.GetCurrentMethod().Name);
                objUser = new User();
                Guid strCRMUserID = new Guid(CRMUserID);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                objUser = (from c in pam2EntitiesContext.Users
                           where c.CRMUserId == strCRMUserID && c.IsDeleted == false
                           select c).FirstOrDefault();
                //if (objUser != null)
                //{
                //    if (objUser.UserRoles.FirstOrDefault() != null)
                //    {
                //        //  objResultValidOrgAndUser.IsValidUser = true;
                //        //  objResultValidOrgAndUser.IsValidOrganisation = true;
                //        strRole = objUser.UserRoles.FirstOrDefault().Role.Name;
                //    }
                //    //else
                //    //{
                //    //    objResultValidOrgAndUser.IsValidUser = true;
                //    //    objResultValidOrgAndUser.IsValidOrganisation = true;
                //    //}
                //}
            }
            catch (Exception ex)
            {
                TraceLog.Write("Error in " + MethodBase.GetCurrentMethod().Name + ": " + ex.ToString(), ex);
                throw ex;
            }

            TraceLog.Write("Start " + MethodBase.GetCurrentMethod().Name);
            return objUser;
        }

        public bool SetTermsAndConditionsApproval(string UserId, bool IsAccepted)
        {
            bool returnValue = false;
            try
            {
                User objUser;
                Guid UserIDGuid = new Guid(UserId);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                objUser = (from c in pam2EntitiesContext.Users
                           where c.UserId == UserIDGuid && c.IsDeleted == false
                           select c).FirstOrDefault();
                if (objUser != null)
                {
                    objUser.AreTermsAccepted = IsAccepted;
                    pam2EntitiesContext.SaveChanges();
                    returnValue = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return returnValue;
        }


        #endregion

        #region PAM Role

        public Role GetPAMRole(string roleName)
        {
            Role retrievedRole = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                retrievedRole = (from role in pam2EntitiesContext.Roles
                                 where role.Name == roleName
                                 select role).ToList<Role>().First<Role>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retrievedRole;
        }

        #endregion

        #region PAM UserRole

        public UserRole AddPAMUserRole(string pamUserId, string roleId)
        {
            UserRole addedUserRole = null;
            UserRole newUserRole = new UserRole();
            try
            {
                newUserRole.RoleId = new Guid(roleId);
                newUserRole.UserId = new Guid(pamUserId);
                if (newUserRole.UserRoleId == Guid.Empty)
                    newUserRole.UserRoleId = Guid.NewGuid();

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                addedUserRole = pam2EntitiesContext.UserRoles.Add(newUserRole);
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedUserRole;
        }

        public UserRole DeletePAMUserRole(string pamUserId, string roleId)
        {
            UserRole userRole = null;
            try
            {
                Guid userIdGuid = new Guid(pamUserId);
                Guid roleIdGuid = new Guid(roleId);

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                userRole = pam2EntitiesContext.UserRoles.Where(s => s.UserId == userIdGuid && s.RoleId == roleIdGuid).FirstOrDefault();
                if (userRole != null)
                {
                    userRole = pam2EntitiesContext.UserRoles.Remove(userRole);
                    int count = pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return userRole;
        }

        //public UserRole UpdatePAMUserRole(string pamUserId, string oldRoleId,string newRoleId)
        //{
        //    UserRole userRole = null;
        //    try
        //    {
        //        Guid userIdGuid = new Guid(pamUserId);
        //        Guid roleIdGuid = new Guid(roleId);

        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        userRole = pam2EntitiesContext.UserRoles.Where(s => s.UserId == userIdGuid && s.RoleId == roleIdGuid).FirstOrDefault();
        //        if (userRole != null)
        //        {
        //            userRole = pam2EntitiesContext.UserRoles.Remove(userRole);
        //            int count = pam2EntitiesContext.SaveChanges();
        //        }
        //    }
        //    catch (Exception excObj)
        //    {
        //        throw excObj;
        //    }
        //    return userRole;
        //}

        #endregion

        #region SmartMerge Setting

        public ResultSet CheckIfSettingsOverlap(string Category, string Description, DateTime StartDate, DateTime EndDate)
        {
            var resultSet = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<SmartMergeConfigurationSetting> settings = (from setting in pam2EntitiesContext.SmartMergeConfigurationSettings
                                                                 // where setting.Status == true
                                                                 select setting).ToList();
                bool invalidInterval = false;
                foreach (SmartMergeConfigurationSetting setting in settings)
                {
                    if (invalidInterval)
                    {
                        break;
                    }
                    if (Category.Trim().ToLower().Equals(setting.Category.ToLower()))
                    {
                        invalidInterval = true;
                        break;
                    }
                    invalidInterval = TimeExtensions.IsBetween(StartDate, Convert.ToDateTime(setting.StartDateTime.ToString()), Convert.ToDateTime(setting.EndDateTime.ToString()));
                    if (invalidInterval)
                    {
                        break;
                    }
                    else
                    {
                        invalidInterval = TimeExtensions.IsBetween(EndDate, Convert.ToDateTime(setting.StartDateTime.ToString()), Convert.ToDateTime(setting.EndDateTime.ToString()));
                        if (invalidInterval)
                        {
                            break;
                        }
                    }
                }
                if (invalidInterval)
                {
                    resultSet.Message = "Success";
                    resultSet.Result = true;
                    resultSet.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return resultSet;
        }

        public ResultSet CheckIfGivenSettingsOverlap(List<PAMSmartMergeConfigurationSetting> SmartMergeConfigurationSettings)
        {
            var resultSet = new ResultSet();
            bool intervalOverlap = false;
            try
            {
                for (int i = 0; i < SmartMergeConfigurationSettings.Count; i++)
                {
                    if (intervalOverlap)
                    {
                        break;
                    }
                    DateTime StartTimeToCheckFor = DateTime.MinValue;
                    DateTime EndTimeToCheckFor = DateTime.MinValue;
                    if (SmartMergeConfigurationSettings[i].StartDateTime != null)
                    {
                        StartTimeToCheckFor = Convert.ToDateTime(SmartMergeConfigurationSettings[i].StartDateTime.Replace("Z",String.Empty));
                    }

                    if (SmartMergeConfigurationSettings[i].EndDateTime != null)
                    {
                        EndTimeToCheckFor = Convert.ToDateTime(SmartMergeConfigurationSettings[i].EndDateTime.Replace("Z", String.Empty));
                    }

                    for (int j = 0; j < SmartMergeConfigurationSettings.Count; j++)
                    {
                        if (intervalOverlap)
                        {
                            break;
                        }
                        if (i == j)
                            continue;
                        DateTime StartTimeAgainstToCheckFor = DateTime.MinValue;
                        if (SmartMergeConfigurationSettings[j].StartDateTime != null)
                        {
                            StartTimeAgainstToCheckFor = Convert.ToDateTime(SmartMergeConfigurationSettings[j].StartDateTime.Replace("Z", String.Empty));
                        }
                        DateTime EndTimeAgainstToCheckFor = DateTime.MinValue;
                        if (SmartMergeConfigurationSettings[j].EndDateTime != null)
                        {
                            EndTimeAgainstToCheckFor = Convert.ToDateTime(SmartMergeConfigurationSettings[j].EndDateTime.Replace("Z", String.Empty));
                        }
                        if (StartTimeAgainstToCheckFor != DateTime.MinValue && EndTimeAgainstToCheckFor != DateTime.MinValue)
                        {
                            intervalOverlap = TimeExtensions.IsBetween(StartTimeToCheckFor, StartTimeAgainstToCheckFor, EndTimeAgainstToCheckFor);
                            if (!intervalOverlap)
                            {
                                intervalOverlap = TimeExtensions.IsBetween(EndTimeToCheckFor, StartTimeAgainstToCheckFor, EndTimeAgainstToCheckFor);
                            }
                        }
                    }
                }

                if (intervalOverlap)
                {
                    resultSet.Message = "Success";
                    resultSet.Result = true;
                    resultSet.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return resultSet;
        }

        public SmartMergeConfigurationSettingResultSet GetSmartMergeConfigurationSettings()
        {
            var smartMergeConfigurationSettingResultSet = new SmartMergeConfigurationSettingResultSet();
            List<PAMSmartMergeConfigurationSetting> smartMergeList;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                smartMergeList = (from smartMergeSetting in pam2EntitiesContext.SmartMergeConfigurationSettings
                                  select smartMergeSetting).AsEnumerable().Select(smartSetting => new PAMSmartMergeConfigurationSetting
                                                    {
                                                        Category = smartSetting.Category,
                                                        //CreatedBy = smartSetting.CreatedBy,
                                                        //CreatedDate = smartSetting.CreatedDate,
                                                        DataValue = smartSetting.DataValue,
                                                        Description = smartSetting.Description,
                                                        EndDateTime = smartSetting.EndDateTime.ToString(),
                                                        SmartMergeConfigId = smartSetting.SmartMergeConfigId.ToString(),
                                                        StartDateTime = smartSetting.StartDateTime.ToString(),
                                                        Status = smartSetting.Status,
                                                        SystemIdleWaitTiimeInMinutes = smartSetting.SystemIdleWaitTiimeInMinutes,
                                                        //  UpdatedBy = smartSetting.UpdatedBy,
                                                        // UpdatedDate = smartSetting.UpdatedDate

                                                    }).ToList();

                smartMergeConfigurationSettingResultSet.Message = "Success";
                smartMergeConfigurationSettingResultSet.Result = true;
                smartMergeConfigurationSettingResultSet.success = true;
                smartMergeConfigurationSettingResultSet.total = smartMergeList != null ? smartMergeList.Count : 0;
                smartMergeConfigurationSettingResultSet.SmartMergeConfigurationSettings = smartMergeList;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return smartMergeConfigurationSettingResultSet;
        }

        public ResultSet UpdateSmartMergeConfigurationSetting(PAMSmartMergeConfigurationSetting updatedSmartMergeSetting, string PAMUserId)
        {
            SmartMergeConfigurationSetting foundSmartMergeSetting = null;
            var result = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid smartMergeSettingId = new Guid(updatedSmartMergeSetting.SmartMergeConfigId);
                foundSmartMergeSetting = pam2EntitiesContext.SmartMergeConfigurationSettings.Where(setting => setting.SmartMergeConfigId == smartMergeSettingId).FirstOrDefault();

                if (foundSmartMergeSetting != null)
                {
                    foundSmartMergeSetting.Category = updatedSmartMergeSetting.Category;
                    foundSmartMergeSetting.DataValue = updatedSmartMergeSetting.DataValue;
                    foundSmartMergeSetting.Description = updatedSmartMergeSetting.Description;
                    foundSmartMergeSetting.StartDateTime = Convert.ToDateTime(updatedSmartMergeSetting.StartDateTime);
                    foundSmartMergeSetting.EndDateTime = Convert.ToDateTime(updatedSmartMergeSetting.EndDateTime);
                    foundSmartMergeSetting.DataValue = updatedSmartMergeSetting.DataValue;
                    foundSmartMergeSetting.SystemIdleWaitTiimeInMinutes = updatedSmartMergeSetting.SystemIdleWaitTiimeInMinutes;
                    foundSmartMergeSetting.Status = updatedSmartMergeSetting.Status;
                    foundSmartMergeSetting.UpdatedBy = new Guid(PAMUserId);
                    foundSmartMergeSetting.UpdatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SaveChanges();
                    result.Result = true;
                    result.success = true;
                    result.Message = "Success";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public ResultSet AddSmartMergeConfigurationSetting(PAMSmartMergeConfigurationSetting addedSmartMergeSetting, string PAMUserId)
        {
            SmartMergeConfigurationSetting foundSmartMergeSetting = null;
            var result = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid smartMergeSettingId = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(addedSmartMergeSetting.SmartMergeConfigId))
                {
                    smartMergeSettingId = new Guid(addedSmartMergeSetting.SmartMergeConfigId);
                    foundSmartMergeSetting = pam2EntitiesContext.SmartMergeConfigurationSettings.Where(setting => setting.SmartMergeConfigId == smartMergeSettingId).FirstOrDefault();
                    if (foundSmartMergeSetting != null)
                    {
                        foundSmartMergeSetting.Category = addedSmartMergeSetting.Category;
                        foundSmartMergeSetting.DataValue = addedSmartMergeSetting.DataValue;
                        foundSmartMergeSetting.Description = addedSmartMergeSetting.Description;
                        foundSmartMergeSetting.StartDateTime = Convert.ToDateTime(addedSmartMergeSetting.StartDateTime);
                        foundSmartMergeSetting.EndDateTime = Convert.ToDateTime(addedSmartMergeSetting.EndDateTime);
                        foundSmartMergeSetting.SystemIdleWaitTiimeInMinutes = Convert.ToInt32(addedSmartMergeSetting.SystemIdleWaitTiimeInMinutes);
                        foundSmartMergeSetting.UpdatedBy = new Guid(PAMUserId);
                        foundSmartMergeSetting.UpdatedDate = DateTime.UtcNow;
                        pam2EntitiesContext.SaveChanges();
                        result.Result = true;
                        result.success = true;
                        result.Message = "Success";
                    }
                    else
                    {
                        SmartMergeConfigurationSetting newSetting = new SmartMergeConfigurationSetting();
                        newSetting.Category = addedSmartMergeSetting.Category;
                        newSetting.DataValue = addedSmartMergeSetting.DataValue;
                        newSetting.Description = addedSmartMergeSetting.Description;
                        newSetting.StartDateTime = Convert.ToDateTime(addedSmartMergeSetting.StartDateTime);
                        newSetting.EndDateTime = Convert.ToDateTime(addedSmartMergeSetting.EndDateTime);
                        newSetting.SystemIdleWaitTiimeInMinutes = Convert.ToInt32(addedSmartMergeSetting.SystemIdleWaitTiimeInMinutes);
                        newSetting.CreatedBy = new Guid(PAMUserId);
                        newSetting.CreatedDate = DateTime.UtcNow;
                        pam2EntitiesContext.SmartMergeConfigurationSettings.Add(newSetting);
                        result.Result = true;
                        result.success = true;
                        result.Message = "Success";
                    }
                }
                else
                {
                    SmartMergeConfigurationSetting newSetting = new SmartMergeConfigurationSetting();
                    newSetting.Category = addedSmartMergeSetting.Category;
                    newSetting.DataValue = addedSmartMergeSetting.DataValue;
                    newSetting.Description = addedSmartMergeSetting.Description;
                    newSetting.StartDateTime = Convert.ToDateTime(addedSmartMergeSetting.StartDateTime);
                    newSetting.EndDateTime = Convert.ToDateTime(addedSmartMergeSetting.EndDateTime);
                    newSetting.SystemIdleWaitTiimeInMinutes = Convert.ToInt32(addedSmartMergeSetting.SystemIdleWaitTiimeInMinutes);
                    newSetting.CreatedBy = new Guid(PAMUserId);
                    newSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SmartMergeConfigurationSettings.Add(newSetting);
                    pam2EntitiesContext.SaveChanges();
                    result.Result = true;
                    result.success = true;
                    result.Message = "Success";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public ResultSet DeleteSmartMergeConfigurationSetting(string deletedSmartMergeSettingId, string PAMUserId)
        {
            SmartMergeConfigurationSetting foundSmartMergeSetting = null;
            var result = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid deletedSmartMergeSettingIdGuid = new Guid(deletedSmartMergeSettingId);
                foundSmartMergeSetting = pam2EntitiesContext.SmartMergeConfigurationSettings.Where(setting => setting.SmartMergeConfigId == deletedSmartMergeSettingIdGuid).FirstOrDefault();
                if (foundSmartMergeSetting != null)
                {
                    pam2EntitiesContext.SmartMergeConfigurationSettings.Remove(foundSmartMergeSetting);
                    pam2EntitiesContext.SaveChanges();
                    result.Result = true;
                    result.success = true;
                    result.Message = "Success";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        #endregion

        #region Rule Builder

        public PAMMatchRuleListResultSet GetAllMatchRules()
        {
            SqlDataReader dr = null;
            List<PAMMatchRule> lstMatchRule = new List<PAMMatchRule>();
            PAMMatchRuleListResultSet MatchRuleResult = new PAMMatchRuleListResultSet();

            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT MatchRuleId, RuleName, Enum, Description from
                                    MatchRuleMaster order by RuleName";
                    //             WHERE IsDeleted=0";
                    dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        PAMMatchRule MatchRuleObj = new PAMMatchRule();
                        MatchRuleObj.MatchRuleId = Convert.ToString(dr["MatchRuleId"]);
                        MatchRuleObj.RuleName = Convert.ToString(dr["RuleName"]);
                        MatchRuleObj.Enum = Convert.ToString(dr["Enum"]);
                        MatchRuleObj.Description = Convert.ToString(dr["Description"]);
                        MatchRuleObj.id = Convert.ToString(dr["MatchRuleId"]);
                        MatchRuleObj.text = Convert.ToString(dr["RuleName"]);
                        MatchRuleObj.leaf = true;
                        MatchRuleObj.qtip = Convert.ToString(dr["RuleName"]);
                        lstMatchRule.Add(MatchRuleObj);
                    }

                    dr.Close();

                    if (_connection.State != ConnectionState.Closed)
                    {
                        _connection.Close();
                    }

                    MatchRuleResult.Message = "Success";
                    MatchRuleResult.MatchRules = lstMatchRule;
                    MatchRuleResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }

                MatchRuleResult.Message = excObj.Message;
                MatchRuleResult.Result = false;
            }

            return MatchRuleResult;
        }

        //        public MatchGroupListResultSet GetAllMatchGroups()
        //        {
        //            SqlDataReader dr = null;
        //            List<MatchGroup> lstMatchGroup = new List<MatchGroup>();
        //            MatchGroupListResultSet MatchGroupResult = new MatchGroupListResultSet();

        //            try
        //            {
        //                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
        //                {
        //                    _connection.Open();
        //                }

        //                using (SqlCommand cmd = _connection.CreateCommand())
        //                {
        //                    cmd.CommandText = @"SELECT MatchGroupId, DisplayName, EntitySettingId, CreatedBy, CreatedDate, UpdatedBy, UpdateDate from
        //                                        MatchGroup ";
        //                    //             WHERE IsDeleted=0";
        //                    dr = cmd.ExecuteReader();

        //                    while (dr.Read())
        //                    {
        //                        MatchGroup MatchGroupObj = new MatchGroup();
        //                        MatchGroupObj.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
        //                        MatchGroupObj.DisplayName = Convert.ToString(dr["DisplayName"]);
        //                        MatchGroupObj.EntitySettingId = Convert.ToString(dr["EntitySettingId"]);
        //                        MatchGroupObj.CreatedBy = Convert.ToString(dr["CreatedBy"]);
        //                        MatchGroupObj.CreatedDate = Convert.ToString(dr["CreatedDate"]);
        //                        MatchGroupObj.UpdatedBy = Convert.ToString(dr["UpdatedBy"]);
        //                        MatchGroupObj.UpdateDate = Convert.ToString(dr["UpdateDate"]);

        //                        lstMatchGroup.Add(MatchGroupObj);
        //                    }

        //                    dr.Close();

        //                    if (_connection.State != ConnectionState.Closed)
        //                    {
        //                        _connection.Close();
        //                    }

        //                    MatchGroupResult.Message = "Success";
        //                    MatchGroupResult.MatchRules = lstMatchGroup;
        //                    MatchGroupResult.Result = true;
        //                }
        //            }
        //            catch (Exception excObj)
        //            {
        //                if (_connection.State != ConnectionState.Closed)
        //                {
        //                    _connection.Close();
        //                }

        //                MatchGroupResult.Message = excObj.Message;
        //                MatchGroupResult.Result = false;
        //            }

        //            return MatchGroupResult;
        //        }

        //        public MatchGroupMasterListResultSet GetAllMatchGroupMasters()
        //        {
        //            SqlDataReader dr = null;
        //            List<MatchGroupMaster> lstMatchGroupMaster = new List<MatchGroupMaster>();
        //            MatchGroupMasterListResultSet MatchGroupMasterResult = new MatchGroupMasterListResultSet();

        //            try
        //            {
        //                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
        //                {
        //                    _connection.Open();
        //                }

        //                using (SqlCommand cmd = _connection.CreateCommand())
        //                {
        //                    cmd.CommandText = @"SELECT MatchGroupId, Name, CreatedBy, CreatedDate, UpdatedBy, UpdateDate from
        //                                        MatchGroupMaster ";
        //                    //             WHERE IsDeleted=0";
        //                    dr = cmd.ExecuteReader();

        //                    while (dr.Read())
        //                    {
        //                        MatchGroupMaster MatchGroupMasterObj = new MatchGroupMaster();
        //                        MatchGroupMasterObj.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
        //                        MatchGroupMasterObj.Name = Convert.ToString(dr["Name"]);
        //                        MatchGroupMasterObj.CreatedBy = Convert.ToString(dr["CreatedBy"]);
        //                        MatchGroupMasterObj.CreatedDate = Convert.ToString(dr["CreatedDate"]);
        //                        MatchGroupMasterObj.UpdatedBy = Convert.ToString(dr["UpdatedBy"]);
        //                        MatchGroupMasterObj.UpdateDate = Convert.ToString(dr["UpdateDate"]);

        //                        lstMatchGroupMaster.Add(MatchGroupMasterObj);
        //                    }

        //                    dr.Close();

        //                    if (_connection.State != ConnectionState.Closed)
        //                    {
        //                        _connection.Close();
        //                    }

        //                    MatchGroupMasterResult.Message = "Success";
        //                    MatchGroupMasterResult.MatchRules = lstMatchGroupMaster;
        //                    MatchGroupMasterResult.Result = true;
        //                }
        //            }
        //            catch (Exception excObj)
        //            {
        //                if (_connection.State != ConnectionState.Closed)
        //                {
        //                    _connection.Close();
        //                }

        //                MatchGroupMasterResult.Message = excObj.Message;
        //                MatchGroupMasterResult.Result = false;
        //            }

        //            return MatchGroupMasterResult;
        //        }


        public GroupRuleDetail GetGropRuleDetailByGroupRuleId(string GroupRuleId)
        {
            GroupRuleDetail objGroupRuleDetail = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                objGroupRuleDetail = pam2EntitiesContext.GroupRuleDetails.Where(x => x.GroupRuleId == new Guid(GroupRuleId)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objGroupRuleDetail;
        }


        public PAMGroupRuleResultSet GetAllMatchGroupRules()
        {
            SqlDataReader dr = null;
            List<PAMGroupRule> lstGroupRule = new List<PAMGroupRule>();
            PAMGroupRuleResultSet objGroupRuleResultSet = new PAMGroupRuleResultSet();

            try
            {

                if (_connection == null)
                {
                    _connection = new SqlConnection(sqlConnString);
                }

                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"Select GroupRuleId, MGM.MatchGroupId, MGM.Name as GroupName, GR.RuleId, MRM.RuleName, MRM.Enum, GR.Description,
                                         GR.IsMaster  from
                                       dbo.MatchGroupMaster MGM left join dbo.GroupRule GR on GR.GroupId=MGM.MatchGroupId 
                                       left join dbo.MatchRuleMaster MRM on GR.RuleId = MRM.MatchRuleId  order by MGM.Name, GR.[Order]";

                    dr = cmd.ExecuteReader();

                    PAMGroupRule GroupRuleObj = new PAMGroupRule();

                    List<PAMGroupRule> lstMatchRule = new List<PAMGroupRule>();
                    string strPreviousGroup = String.Empty;
                    int i = 1;

                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            PAMGroupRule objMatchRule = new PAMGroupRule();

                            if (String.IsNullOrEmpty(strPreviousGroup) || String.Compare(strPreviousGroup, Convert.ToString(dr["MatchGroupId"])) != 0)
                            {
                                if (!String.IsNullOrEmpty(strPreviousGroup))
                                {
                                    GroupRuleObj.leaf = false;
                                    lstGroupRule.Add(GroupRuleObj);
                                    GroupRuleObj = new PAMGroupRule();
                                    GroupRuleObj.children = new List<PAMGroupRule>();
                                }

                                lstMatchRule = new List<PAMGroupRule>();
                                GroupRuleObj.id = i++.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                                GroupRuleObj.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                                GroupRuleObj.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                                GroupRuleObj.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);
                                GroupRuleObj.text = Convert.ToString(dr["GroupName"]);
                                GroupRuleObj.qtip = Convert.ToString(dr["GroupName"]);
                                GroupRuleObj.cls = "folder";
                            }

                            if (Convert.ToString(dr["RuleId"]) != String.Empty)
                            {
                                objMatchRule.id = Convert.ToString(dr["GroupRuleId"]);
                                objMatchRule.MatchRuleId = Convert.ToString(dr["RuleId"]);

                                objMatchRule.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                                objMatchRule.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                                objMatchRule.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);

                                objMatchRule.text = Convert.ToString(dr["RuleName"]);
                                objMatchRule.RuleName = Convert.ToString(dr["RuleName"]);
                                objMatchRule.Enum = Convert.ToString(dr["Enum"]);

                                if (objMatchRule.Enum == DataEnums.Rules.CustomTransformLibrary.ToString())
                                {
                                    GroupRuleDetail obj = GetGropRuleDetailByGroupRuleId(Convert.ToString(dr["GroupRuleId"]));
                                    if (obj != null)
                                        objMatchRule.id = Convert.ToString(dr["GroupRuleId"]) + "_" + obj.AttributeValue;
                                }
                                objMatchRule.Description = Convert.ToString(dr["Description"]);
                                objMatchRule.qtip = Convert.ToString(dr["Description"]);
                                objMatchRule.cls = "file";
                                objMatchRule.leaf = true;
                                lstMatchRule.Add(objMatchRule);

                                GroupRuleObj.children = lstMatchRule;
                            }

                            strPreviousGroup = Convert.ToString(dr["MatchGroupId"]);
                        }

                        GroupRuleObj.leaf = false;
                        lstGroupRule.Add(GroupRuleObj);
                    }

                }

                dr.Close();

                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }

                objGroupRuleResultSet.Message = "Success";
                objGroupRuleResultSet.GroupRules = lstGroupRule;
                objGroupRuleResultSet.Result = true;
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }

                objGroupRuleResultSet.Message = excObj.Message;
                objGroupRuleResultSet.Result = false;
            }

            return objGroupRuleResultSet;
        }


        public PAMGroupRuleResultSet GetAllMatchGroupRules(string SessionId)
        {
            SqlDataReader dr = null;
            List<PAMGroupRule> lstGroupRule = new List<PAMGroupRule>();
            PAMGroupRuleResultSet objGroupRuleResultSet = new PAMGroupRuleResultSet();

            try
            {
                if (_connection == null)
                {
                    _connection = new SqlConnection(sqlConnString);
                }

                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    //                    cmd.CommandText = @"select GR.GroupRuleId,GR.GroupID MatchGroupId,isnull(MGM.DisplayName,'NA') GroupName, GR.RuleId,MRM.RuleName,MRM.Enum,GR.Description,GR.IsMaster,GR.[Order] from GroupRule GR
                    //                                        inner join MatchGroup MGM on MGM.MatchGroupId = GR.GroupID
                    //                                        inner join MatchRuleMaster MRM on MRM.MatchRuleId = GR.RuleId
                    //                                        where  GroupId in (select MatchGroupId from SessionGroup where sessionID=@sessionId) order by MGM.DisplayName,GR.[Order]";

                    cmd.CommandText = @"select GR.GroupRuleId,MGM.MatchGroupId,isnull(MGM.DisplayName,'NA') GroupName, GR.RuleId,MRM.RuleName,MRM.Enum,GR.Description,GR.IsMaster,GR.[Order] ,SG.MatchKeyID
                                        from MatchGroup MGM 
                                        left join GroupRule GR on  GR.GroupID = MGM.MatchGroupId
                                        left join MatchRuleMaster MRM on MRM.MatchRuleId = GR.RuleId
                                        left join SessionGroup SG on SG.MatchGroupId = MGM.MatchGroupId
                                        where  MGM.MatchGroupId in (select MatchGroupId from SessionGroup where sessionID=@sessionId)
										order by MGM.DisplayName,GR.[Order]";

                    cmd.Parameters.AddWithValue("@sessionId", SessionId);
                    dr = cmd.ExecuteReader();

                    PAMGroupRule GroupRuleObj = new PAMGroupRule();

                    List<PAMGroupRule> lstMatchRule = new List<PAMGroupRule>();
                    string strPreviousGroup = String.Empty;
                    int i = 1;
                    while (dr.Read())
                    {
                        PAMGroupRule objMatchRule = new PAMGroupRule();

                        if (String.IsNullOrEmpty(strPreviousGroup) || String.Compare(strPreviousGroup, Convert.ToString(dr["MatchGroupId"])) != 0)
                        {
                            if (!String.IsNullOrEmpty(strPreviousGroup))
                            {
                                GroupRuleObj.leaf = false;
                                lstGroupRule.Add(GroupRuleObj);
                                GroupRuleObj = new PAMGroupRule();
                                GroupRuleObj.children = new List<PAMGroupRule>();
                            }

                            lstMatchRule = new List<PAMGroupRule>();
                            GroupRuleObj.id = i++.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                            GroupRuleObj.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                            GroupRuleObj.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                            GroupRuleObj.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);
                            GroupRuleObj.text = Convert.ToString(dr["GroupName"]);
                            GroupRuleObj.qtip = Convert.ToString(dr["GroupName"]);
                            GroupRuleObj.MatchKey = Convert.ToString(dr["MatchKeyID"]);
                            GroupRuleObj.cls = "folder";
                        }

                        if (Convert.ToString(dr["RuleId"]) != String.Empty)
                        {
                            objMatchRule.id = Convert.ToString(dr["GroupRuleId"]);
                            objMatchRule.MatchRuleId = Convert.ToString(dr["RuleId"]);

                            objMatchRule.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                            objMatchRule.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                            objMatchRule.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);

                            objMatchRule.text = Convert.ToString(dr["RuleName"]);
                            objMatchRule.RuleName = Convert.ToString(dr["RuleName"]);
                            objMatchRule.Enum = Convert.ToString(dr["Enum"]);

                            if (objMatchRule.Enum == DataEnums.Rules.CustomTransformLibrary.ToString())
                            {
                                GroupRuleDetail obj = GetGropRuleDetailByGroupRuleId(Convert.ToString(dr["GroupRuleId"]));
                                if (obj != null)
                                    objMatchRule.id = Convert.ToString(dr["GroupRuleId"]) + "_" + obj.AttributeValue;
                            }

                            objMatchRule.Description = Convert.ToString(dr["Description"]);
                            objMatchRule.qtip = Convert.ToString(dr["Description"]);
                            objMatchRule.cls = "file";
                            objMatchRule.leaf = true;
                            lstMatchRule.Add(objMatchRule);

                            GroupRuleObj.children = lstMatchRule;
                        }

                        strPreviousGroup = Convert.ToString(dr["MatchGroupId"]);
                    }
                    GroupRuleObj.leaf = false;
                    lstGroupRule.Add(GroupRuleObj);
                }
                dr.Close();
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }

                objGroupRuleResultSet.Message = "Success";
                objGroupRuleResultSet.GroupRules = lstGroupRule;
                objGroupRuleResultSet.Result = true;
                objGroupRuleResultSet.success = true;
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objGroupRuleResultSet.Message = excObj.Message;
                objGroupRuleResultSet.Result = false;
            }
            return objGroupRuleResultSet;
        }

        public PAMRuleDropDownDetailResultSet GetAllRuleDropDownDetails()
        {
            SqlDataReader dr = null;
            List<PAMRuleDropDownDetail> lstRuleDropDownDetail = new List<PAMRuleDropDownDetail>();
            PAMRuleDropDownDetailResultSet objRuleDropDownDetailResultSet = new PAMRuleDropDownDetailResultSet();
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  [DropDownDetailId], MRDetail.[DropDownId],MR.Enum DropDownEnum
                                    ,MRDetail.[Enum],[Value],[ParentId]
                                    FROM [dbo].MatchRuleDropDown MR
                                    inner join dbo.[MatchRuleDropDownDetail] MRDetail
                                    on MR.DropDownId = MRDetail.DropDownId ";
                    //             WHERE IsDeleted=0";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        PAMRuleDropDownDetail objRuleDropDownDetail = new PAMRuleDropDownDetail();
                        objRuleDropDownDetail.DropDownDetailId = Convert.ToString(dr["DropDownDetailId"]);
                        objRuleDropDownDetail.DropDownId = Convert.ToString(dr["DropDownId"]);
                        objRuleDropDownDetail.DropDownEnum = Convert.ToString(dr["DropDownEnum"]);
                        objRuleDropDownDetail.Enum = Convert.ToString(dr["Enum"]);
                        objRuleDropDownDetail.Value = Convert.ToString(dr["Value"]);
                        objRuleDropDownDetail.ParentId = Convert.ToString(dr["ParentId"]);
                        lstRuleDropDownDetail.Add(objRuleDropDownDetail);
                    }
                    dr.Close();

                    if (_connection.State != ConnectionState.Closed)
                    {
                        _connection.Close();
                    }

                    objRuleDropDownDetailResultSet.Message = "Success";
                    objRuleDropDownDetailResultSet.RuleDropDownDetails = lstRuleDropDownDetail;
                    objRuleDropDownDetailResultSet.Result = true;
                }
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objRuleDropDownDetailResultSet.Message = excObj.Message;
                objRuleDropDownDetailResultSet.Result = false;
            }
            return objRuleDropDownDetailResultSet;
        }

        public PAMRuleDropDownDetailResultSet GetFilteredRuleDropDownDetails(string DropDownEnum, bool ByParentId)
        {
            SqlDataReader dr = null;
            List<PAMRuleDropDownDetail> lstRuleDropDownDetail = new List<PAMRuleDropDownDetail>();
            PAMRuleDropDownDetailResultSet objRuleDropDownDetailResultSet = new PAMRuleDropDownDetailResultSet();

            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    if (ByParentId)
                    {
                        cmd.CommandText = @"select MDetail2.*,MR.Enum DropDownEnum from 
                                    dbo.MatchRuleDropDownDetail MDetail1 inner join dbo.MatchRuleDropDownDetail MDetail2
                                    on MDetail1.DropDownDetailId =  MDetail2.ParentId
                                    inner join  [dbo].MatchRuleDropDown MR on MR.DropDownId=MDetail2.DropDownId
                                    where MDetail1.Enum = '" + DropDownEnum + "' order by MDetail2.Value ";
                    }
                    else
                    {
                        cmd.CommandText = @"SELECT  [DropDownDetailId], MRDetail.[DropDownId],MR.Enum DropDownEnum
                                    ,MRDetail.[Enum],[Value],[ParentId]
                                    FROM [dbo].MatchRuleDropDown MR
                                    inner join dbo.[MatchRuleDropDownDetail] MRDetail
                                    on MR.DropDownId = MRDetail.DropDownId 
                                    where MR.Enum = '" + DropDownEnum + "' order by MRDetail.Value ";
                        //             WHERE IsDeleted=0";
                    }
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        PAMRuleDropDownDetail objRuleDropDownDetail = new PAMRuleDropDownDetail();
                        objRuleDropDownDetail.DropDownDetailId = Convert.ToString(dr["DropDownDetailId"]);
                        objRuleDropDownDetail.DropDownId = Convert.ToString(dr["DropDownId"]);
                        objRuleDropDownDetail.DropDownEnum = Convert.ToString(dr["DropDownEnum"]);
                        objRuleDropDownDetail.Enum = Convert.ToString(dr["Enum"]);
                        objRuleDropDownDetail.Value = Convert.ToString(dr["Value"]);
                        objRuleDropDownDetail.ParentId = Convert.ToString(dr["ParentId"]);
                        lstRuleDropDownDetail.Add(objRuleDropDownDetail);
                    }
                    dr.Close();
                    if (_connection.State != ConnectionState.Closed)
                    {
                        _connection.Close();
                    }
                    objRuleDropDownDetailResultSet.Message = "Success";
                    objRuleDropDownDetailResultSet.RuleDropDownDetails = lstRuleDropDownDetail;
                    objRuleDropDownDetailResultSet.Result = true;
                }
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objRuleDropDownDetailResultSet.Message = excObj.Message;
                objRuleDropDownDetailResultSet.Result = false;
            }
            return objRuleDropDownDetailResultSet;
        }

        public PAMRuleDropDownDetailResultSet GetDropDownDetailsbyParentEnum(string DropDownDetailEnum)
        {
            SqlDataReader dr = null;
            List<PAMRuleDropDownDetail> lstRuleDropDownDetail = new List<PAMRuleDropDownDetail>();
            PAMRuleDropDownDetailResultSet objRuleDropDownDetailResultSet = new PAMRuleDropDownDetailResultSet();
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }
                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = @"select MDetail2.*,MR.Enum DropDownEnum from 
                                    dbo.MatchRuleDropDownDetail MDetail1 inner join dbo.MatchRuleDropDownDetail MDetail2
                                    on MDetail1.DropDownDetailId =  MDetail2.ParentId
                                    inner join  [dbo].MatchRuleDropDown MR on MR.DropDownId=MDetail2.DropDownId
                                    where MRDetail.Enum = '" + DropDownDetailEnum + "'";
                    //             WHERE IsDeleted=0";
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        PAMRuleDropDownDetail objRuleDropDownDetail = new PAMRuleDropDownDetail();
                        objRuleDropDownDetail.DropDownDetailId = Convert.ToString(dr["DropDownDetailId"]);
                        objRuleDropDownDetail.DropDownId = Convert.ToString(dr["DropDownId"]);
                        objRuleDropDownDetail.DropDownEnum = Convert.ToString(dr["DropDownEnum"]);
                        objRuleDropDownDetail.Enum = Convert.ToString(dr["Enum"]);
                        objRuleDropDownDetail.Value = Convert.ToString(dr["Value"]);
                        objRuleDropDownDetail.ParentId = Convert.ToString(dr["ParentId"]);
                        lstRuleDropDownDetail.Add(objRuleDropDownDetail);
                    }
                    dr.Close();
                    if (_connection.State != ConnectionState.Closed)
                    {
                        _connection.Close();
                    }
                    objRuleDropDownDetailResultSet.Message = "Success";
                    objRuleDropDownDetailResultSet.RuleDropDownDetails = lstRuleDropDownDetail;
                    objRuleDropDownDetailResultSet.Result = true;
                }
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objRuleDropDownDetailResultSet.Message = excObj.Message;
                objRuleDropDownDetailResultSet.Result = false;
            }
            return objRuleDropDownDetailResultSet;
        }

        public ResultSet SaveMasterGroupRules(List<PAMGroupRule> GroupRules, string PAMUserId)
        {
            ResultSet objResultSet = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                // Get already created match groups
                List<MatchGroupMaster> lstMatchGroup = pam2EntitiesContext.MatchGroupMasters.ToList<MatchGroupMaster>();
                //List<MatchGroupMaster> lstMatchGroupFiltered = lstMatchGroup.Where(p => GroupRules.Any(i => i.text.ToLower().Trim() == p.Name.ToLower().Trim())).ToList<MatchGroupMaster>();
                List<MatchGroupMaster> lstMatchGroupFiltered = lstMatchGroup.Where(p => GroupRules.Any(i => i.MatchGroupId.ToLower().Trim() == p.MatchGroupId.ToString().ToLower().Trim())).ToList<MatchGroupMaster>();

                List<GroupRule> lstGroupRule = (from c in pam2EntitiesContext.GroupRules
                                                join
                                                    d in pam2EntitiesContext.MatchGroupMasters on c.GroupId equals d.MatchGroupId
                                                select c).ToList<GroupRule>();

                //   List<SessionGroup> lstSessionGroups = pam2EntitiesContext.SessionGroups.Where(p => p.SessionId == SessionGuid).ToList<SessionGroup>();
                //   List<SessionGroup> SessionGroups = lstSessionGroups.Where(p => GroupRules.Any(i => i.text.ToLower().Trim() == p.MatchGroup.DisplayName.ToLower().Trim())).ToList<SessionGroup>();
                //   List<GroupRule> lstGroupRules = (from c in pam2EntitiesContext.GroupRules
                //                                 join
                //                                    d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                //                                 where d.SessionId == SessionGuid
                //                                 select c).ToList<GroupRule>();

                //List<GroupRuleDetail> lstGroupRuleDetail = (from e in pam2EntitiesContext.GroupRuleDetails
                //                                            join c in pam2EntitiesContext.GroupRules on e.GroupRuleId equals c.GroupRuleId
                //                                            join d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                //                                            where d.SessionId == SessionGuid
                //                                            select e).ToList<GroupRuleDetail>();

                // pam2EntitiesContext.SaveChanges();

                if (_connection == null)
                {
                    _connection = new SqlConnection(sqlConnString);
                }

                foreach (GroupRule objGroupRule in lstGroupRule)
                {
                    if (objGroupRule.MatchRuleMaster.Enum == DataEnums.Rules.CustomTransformLibrary.ToString())
                    {
                        GroupRuleDetail objGroupRuleDetail = objGroupRule.GroupRuleDetails.ElementAt(0);
                        if (objGroupRuleDetail != null)
                        {
                            DeleteCTLCategory(objGroupRuleDetail.AttributeValue);
                        }
                    }

                    DeleteGroupRuleRecord(Convert.ToString(objGroupRule.GroupRuleId));
                }

                List<MatchGroupMaster> lstMatchGroupDeleted = lstMatchGroup.Where(p => GroupRules.All(i => i.MatchGroupId.ToLower().Trim() != p.MatchGroupId.ToString().ToLower().Trim())).ToList<MatchGroupMaster>();

                foreach (var objMatchGroupMaster in lstMatchGroupDeleted)
                {
                    DeleteMatchGroup(objMatchGroupMaster.MatchGroupId.ToString());
                }

                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    foreach (var objGroupRule in GroupRules)
                    {
                        int iOrder = 0;
                        // string GroupName = "";
                        //MatchGroupMaster objMatchGroup = lstMatchGroupFiltered.Where(p => p.Name.ToLower().Trim() == objGroupRule.text.Trim().ToLower()).FirstOrDefault();
                        MatchGroupMaster objMatchGroup = lstMatchGroupFiltered.Where(p => p.MatchGroupId.ToString().ToLower().Trim() == objGroupRule.MatchGroupId.Trim().ToLower()).FirstOrDefault();

                        if (objMatchGroup == null)
                        {
                            AddMasterMatchGroups(objGroupRule.text.Trim(), PAMUserId, out objMatchGroup);
                        }
                        else
                        {
                            if (objMatchGroup.Name.ToLower().Trim() != objGroupRule.text.ToLower().Trim())
                            {
                                UpdateMasterMatchGroup(objMatchGroup.MatchGroupId.ToString(), objGroupRule.text.Trim(), PAMUserId);
                            }
                        }

                        //if (objMatchGroup != null)
                        //    GroupName = objMatchGroup.DisplayName;
                        foreach (var objGrouprule1 in objGroupRule.children)
                        {
                            Guid GroupRuleId = Guid.NewGuid();
                            cmd.CommandText = @"INSERT INTO [dbo].[GroupRule] ([GroupRuleId],[GroupId],[RuleId],[IsMaster],[Order],[Description]) VALUES (@GroupRuleId,@GroupId,@RuleId,1,@Order,@Description)";
                            cmd.Parameters.AddWithValue("@GroupRuleId", GroupRuleId);
                            cmd.Parameters.AddWithValue("@GroupId", objMatchGroup.MatchGroupId);
                            cmd.Parameters.AddWithValue("@RuleId", objGrouprule1.MatchRuleId);
                            cmd.Parameters.AddWithValue("@Order", iOrder);
                            cmd.Parameters.AddWithValue("@Description", objGrouprule1.Description);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                            string strDescription = objGrouprule1.Description;
                            ArrayList arrAttr = new ArrayList();
                            ArrayList arrAttrValues = new ArrayList();

                            switch (objGrouprule1.Enum)
                            {
                                case "CustomExclude":
                                    // @@WholeWords (@@LookFor->@@ChangeTo)
                                    arrAttr.Add(DataEnums.RuleAttributes.LeftDelimiter.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RightDelimiter.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.Mode.ToString());

                                    if (strDescription.Contains("Remove Between Delimiters"))
                                        strDescription = strDescription.Replace("Remove Between Delimiters", "RBD");

                                    if (strDescription.Contains("Remove Delimiters Only"))
                                        strDescription = strDescription.Replace("Remove Delimiters Only", "RDO");

                                    if (strDescription.Contains("Remove Delimiters and Between"))
                                        strDescription = strDescription.Replace("Remove Delimiters and Between", "RDAB");

                                    if (strDescription.Contains("Remove Outside the Delimiters"))
                                        strDescription = strDescription.Replace("Remove Outside the Delimiters", "ROD");

                                    int iIndex = strDescription.IndexOf("(");
                                    string strDescriptionData = strDescription.Substring(iIndex, strDescription.Length - iIndex);
                                    string strMode = strDescription.Replace(strDescriptionData, String.Empty);
                                    strDescriptionData = strDescriptionData.Replace("('", String.Empty).Replace("')", String.Empty);
                                    strDescriptionData = strDescriptionData.Replace("'->'", ">");
                                    //  char[] charParam = new char[] { '-', '>' };
                                    string[] strData = strDescriptionData.Split('>');
                                    //  string[] strData = strDescriptionData.Split(charParam);
                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                        arrAttrValues.Add(strMode);
                                    }

                                    break;

                                case "CustomTransform":
                                    // @@WholeWords (@@LookFor->@@ChangeTo)
                                    arrAttr.Add(DataEnums.RuleAttributes.LookFor.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.ChangeTo.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.WholeWords.ToString());

                                    iIndex = strDescription.IndexOf("(");
                                    strDescriptionData = strDescription.Substring(iIndex, strDescription.Length - iIndex);
                                    string strWholeWords = strDescription.Replace(strDescriptionData, String.Empty).Trim();
                                    if (String.IsNullOrEmpty(strWholeWords))
                                    {
                                        strWholeWords = "0";
                                    }
                                    else
                                    {
                                        strWholeWords = "1";
                                    }

                                    strDescriptionData = strDescriptionData.Replace("('", String.Empty).Replace("')", String.Empty);
                                    //  charParam = new char[] { '-', '>' };
                                    //  strData = strDescriptionData.Split(charParam);

                                    strDescriptionData = strDescriptionData.Replace("'->'", ">");
                                    strData = strDescriptionData.Split('>');

                                    if (strData.Length > 1)
                                    {
                                        string strLookFor = strData[0].ToString();
                                        //   strLookFor = strLookFor.Substring(1, strLookFor.Length - 2);
                                        arrAttrValues.Add(strLookFor);

                                        string strChangeTo = strData[1].ToString();
                                        //   strChangeTo = strChangeTo.Substring(1, strChangeTo.Length - 2);
                                        arrAttrValues.Add(strChangeTo);

                                        arrAttrValues.Add(strWholeWords);
                                    }

                                    break;

                                case "ExtractLetters":
                                    // @@ELDirection @@ELNumber letters
                                    arrAttr.Add(DataEnums.RuleAttributes.ELDirection.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.ELNumber.ToString());

                                    strDescription = strDescription.Replace("Letter(s)", String.Empty);
                                    strData = strDescription.Split(' ');

                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                    }

                                    break;

                                case "ExtractWord":
                                    // @@EWDirection @@EWNumber words
                                    arrAttr.Add(DataEnums.RuleAttributes.EWDirection.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.EWNumber.ToString());

                                    strDescription = strDescription.Replace("Word(s)", String.Empty);
                                    strData = strDescription.Split(' ');

                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                    }

                                    break;

                                case "ExtractName":
                                    // @@ExtractName
                                    arrAttr.Add(DataEnums.RuleAttributes.ExtractName.ToString());
                                    if (strDescription.ToUpper().Contains("FIRST NAME") || strDescription.ToUpper().Contains("MIDDLE NAME") || strDescription.ToUpper().Contains("LAST NAME"))
                                    {
                                        strDescription = strDescription.Replace(" name", "name");
                                    }

                                    if (strDescription.ToUpper().Contains("SUFFIX OR QUALIFICATION"))
                                    {
                                        strDescription = "SUFFIX";
                                    }

                                    if (strDescription.ToUpper().Contains("PREFIX OR TITLE"))
                                    {
                                        strDescription = "PREFIX";
                                    }


                                    arrAttrValues.Add(strDescription);
                                    break;

                                case "RemoveChars":
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvVowels.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvConsonants.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvNumbers.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvPunctuation.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvOtherChars.ToString());

                                    if (strDescription.Contains("Other Characters"))
                                    {
                                        strDescription = strDescription.Replace("Other Characters", "OtherChars");
                                    }

                                    for (int i = 0; i < arrAttr.Count; i++)
                                    {
                                        string strAttr = Convert.ToString(arrAttr[i]).Replace("Remv", String.Empty);
                                        if (strDescription.Contains(strAttr))
                                            arrAttrValues.Add(true);
                                        else
                                            arrAttrValues.Add(false);
                                    }

                                    break;

                                case "Normalise":
                                    arrAttr.Add(DataEnums.RuleAttributes.Method.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.Category.ToString());

                                    if (strDescription.Contains("First Names"))
                                    {
                                        strDescription = strDescription.Replace("First Names", "PersonalName");
                                    }

                                    if (strDescription.Contains("Job Titles"))
                                    {
                                        strDescription = strDescription.Replace("Job Titles", "BusinessJobTitle");
                                    }

                                    if (strDescription.Contains("Dates and Events"))
                                    {
                                        strDescription = strDescription.Replace("Dates and Events", "Dates");
                                    }

                                    if (strDescription.Contains("Weights and Measures"))
                                    {
                                        strDescription = strDescription.Replace("Weights and Measures", "WeightsMeasures");
                                    }

                                    strData = strDescription.Split(' ');

                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                    }

                                    break;

                                case "CustomTransformLibrary":
                                    arrAttr.Add(DataEnums.RuleAttributes.CTLCategory.ToString());
                                    string id = objGrouprule1.id;
                                    string[] strArr = id.Split('_');
                                    string strCategoryId = String.Empty;

                                    if (strArr.Length > 1)
                                    {
                                        strCategoryId = strArr[1].ToString();
                                    }

                                    arrAttrValues.Add(strCategoryId);
                                    break;
                                //case "TrimString":
                                //    strRuleNewDesc = strRuleDesc;
                                //    break;
                            }

                            // Insert in GroupRuleDetail table
                            for (int i = 0; i < arrAttr.Count; i++)
                            {
                                cmd.CommandText = @"INSERT INTO [dbo].[GroupRuleDetail] ([GroupRuleDetailId],[GroupRuleId],[AttributeEnum],[AttributeValue]) 
                                        VALUES (@GroupRuleDetailId,@GroupRuleId,@AttributeEnum,@AttributeValue)";
                                cmd.Parameters.AddWithValue("@GroupRuleDetailId", Guid.NewGuid());
                                cmd.Parameters.AddWithValue("@GroupRuleId", GroupRuleId);
                                cmd.Parameters.AddWithValue("@AttributeEnum", Convert.ToString(arrAttr[i]));
                                cmd.Parameters.AddWithValue("@AttributeValue", Convert.ToString(arrAttrValues[i]));
                                cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();
                            }

                            iOrder++;
                        }
                    }
                }
                objResultSet.success = true;
            }
            catch (Exception ex)
            {
                objResultSet.success = false;
                objResultSet.Message = ex.ToString();
            }

            return objResultSet;



            /*
            ResultSet objResultSet = new ResultSet();
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

               
                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    ArrayList arrAttr = new ArrayList();
                    ArrayList arrAttrValues = new ArrayList();
                    arrAttrValues.Add(Param1); arrAttrValues.Add(Param2); arrAttrValues.Add(Param3); arrAttrValues.Add(Param4); arrAttrValues.Add(Param5);
                    string strEnum = "", strRuleDesc = "", strRuleNewDesc = "";
                    cmd.CommandText = "Select Enum, [Description] from MatchRuleMaster where MatchRuleId='" + RuleId + "'";
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        strEnum = Convert.ToString(dr["Enum"]);
                        strRuleDesc = Convert.ToString(dr["Description"]);
                    }
                    dr.Close();
                    dr.Dispose();
                    // Get the Order of last node
                    cmd.CommandText = @"Select max([Order]) from [dbo].[GroupRule] where [GroupId]=@GroupId";
                    cmd.Parameters.AddWithValue("@GroupId", GroupId);
                    object objOrder = cmd.ExecuteScalar();
                    if (objOrder == DBNull.Value)
                        objOrder = 0;

                    cmd.Parameters.Clear();
                    int iOrder = Convert.ToInt32(objOrder) + 1;
                    switch (strEnum)
                    {
                        case "CustomExclude":
                            arrAttr.Add(DataEnums.RuleAttributes.LeftDelimiter.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.RightDelimiter.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.Mode.ToString());
                            strRuleNewDesc = strRuleDesc.Replace("@@" + DataEnums.RuleAttributes.Mode.ToString(), Convert.ToString(arrAttrValues[2]));
                            strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.LeftDelimiter.ToString(), ProperCase(Convert.ToString(arrAttrValues[0])));
                            strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.RightDelimiter.ToString(), ProperCase(Convert.ToString(arrAttrValues[1])));
                            break;
                        case "CustomTransform":
                            arrAttr.Add(DataEnums.RuleAttributes.LookFor.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.ChangeTo.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.WholeWords.ToString());
                            strRuleNewDesc = strRuleDesc.Replace("@@" + DataEnums.RuleAttributes.LookFor.ToString(), ProperCase(Convert.ToString(arrAttrValues[0])));
                            strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.ChangeTo.ToString(), ProperCase(Convert.ToString(arrAttrValues[1])));
                            if (Convert.ToBoolean(arrAttrValues[2]))
                                strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.WholeWords.ToString(), DataEnums.RuleAttributes.WholeWords.ToString());
                            else
                                strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.WholeWords.ToString(), String.Empty);
                            break;
                        case "ExtractLetters":
                            arrAttr.Add(DataEnums.RuleAttributes.ELDirection.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.ELNumber.ToString());
                            strRuleNewDesc = strRuleDesc.Replace("@@" + DataEnums.RuleAttributes.ELDirection.ToString(), ProperCase(Convert.ToString(arrAttrValues[0])));
                            //if (String.IsNullOrEmpty(Convert.ToString(arrAttrValues[1])))
                            //    arrAttrValues[1] = "0";
                            strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.ELNumber.ToString(), ProperCase(Convert.ToString(arrAttrValues[1])));
                            break;
                        case "ExtractWord":
                            arrAttr.Add(DataEnums.RuleAttributes.EWDirection.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.EWNumber.ToString());
                            strRuleNewDesc = strRuleDesc.Replace("@@" + DataEnums.RuleAttributes.EWDirection.ToString(), ProperCase(Convert.ToString(arrAttrValues[0])));
                            strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.EWNumber.ToString(), ProperCase(Convert.ToString(arrAttrValues[1])));
                            break;
                        case "ExtractName":
                            arrAttr.Add(DataEnums.RuleAttributes.ExtractName.ToString());
                            strRuleNewDesc = strRuleDesc.Replace("@@" + DataEnums.RuleAttributes.ExtractName.ToString(), ProperCase(Convert.ToString(arrAttrValues[0])));
                            if (strRuleNewDesc.Contains("FIRST NAME") || strRuleNewDesc.Contains("MIDDLE NAME") || strRuleNewDesc.Contains("LAST NAME"))
                            {
                                strRuleNewDesc = strRuleNewDesc.Replace(" NAME", "NAME");
                            }
                            break;
                        case "RemoveChars":
                            arrAttr.Add(DataEnums.RuleAttributes.RemvVowels.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.RemvConsonants.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.RemvNumbers.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.RemvPunctuation.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.RemvOtherChars.ToString());
                            for (int i = 0; i < arrAttrValues.Count; i++)
                            {
                                if (Convert.ToBoolean(arrAttrValues[i]))
                                {
                                    strRuleNewDesc += Convert.ToString(arrAttr[i]).Replace("Remv", String.Empty) + ",";
                                }
                            }
                            strRuleNewDesc = strRuleNewDesc.Substring(0, strRuleNewDesc.Length - 1);
                            break;
                        case "Normalise":
                            arrAttr.Add(DataEnums.RuleAttributes.Method.ToString());
                            arrAttr.Add(DataEnums.RuleAttributes.Category.ToString());
                            strRuleNewDesc = strRuleDesc.Replace("@@" + DataEnums.RuleAttributes.Method.ToString(), ProperCase(Convert.ToString(arrAttrValues[0])));
                            strRuleNewDesc = strRuleNewDesc.Replace("@@" + DataEnums.RuleAttributes.Category.ToString(), ProperCase(Convert.ToString(arrAttrValues[1])));
                            break;
                        case "TrimString":
                            strRuleNewDesc = strRuleDesc;
                            break;
                    }
                    // Insert in GroupRule table
                    Guid GroupRuleId = Guid.NewGuid();
                    cmd.CommandText = @"INSERT INTO [dbo].[GroupRule] ([GroupRuleId],[GroupId],[RuleId],[IsMaster],[Order],[Description]) VALUES (@GroupRuleId,@GroupId,@RuleId,1,@Order,@Description)";
                    cmd.Parameters.AddWithValue("@GroupRuleId", GroupRuleId);
                    cmd.Parameters.AddWithValue("@GroupId", GroupId);
                    cmd.Parameters.AddWithValue("@RuleId", RuleId);
                    cmd.Parameters.AddWithValue("@Order", iOrder);
                    cmd.Parameters.AddWithValue("@Description", strRuleNewDesc);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    // Insert in GroupRuleDetail table
                    for (int i = 0; i < arrAttr.Count; i++)
                    {
                        cmd.CommandText = @"INSERT INTO [dbo].[GroupRuleDetail] ([GroupRuleDetailId],[GroupRuleId],[AttributeEnum],[AttributeValue]) 
                                    VALUES (@GroupRuleDetailId,@GroupRuleId,@AttributeEnum,@AttributeValue)";
                        cmd.Parameters.AddWithValue("@GroupRuleDetailId", Guid.NewGuid());
                        cmd.Parameters.AddWithValue("@GroupRuleId", GroupRuleId);
                        cmd.Parameters.AddWithValue("@AttributeEnum", Convert.ToString(arrAttr[i]));
                        cmd.Parameters.AddWithValue("@AttributeValue", Convert.ToString(arrAttrValues[i]));
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }
                    objResultSet.Message = "Success";
                    objResultSet.Result = true;
                }
                 * 
                
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objResultSet.Message = excObj.Message;
                objResultSet.Result = false;
            }
            return objResultSet;  
             *  * */
        }

        private string ProperCase(string Word)
        {
            string tempWord = Word;
            if (!String.IsNullOrEmpty(tempWord))
            {
                tempWord = tempWord.ToLower();
                string strFirstAlpha = tempWord[0].ToString().ToUpper();
                tempWord = tempWord.Substring(1, tempWord.Length - 1);
                tempWord = strFirstAlpha + tempWord;
            }
            return tempWord;
        }

        public ResultSet DeleteGroupRuleRecord(string RecordId)
        {
            ResultSet objResultSet = new ResultSet();
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                if (RecordId.Contains("_"))
                {
                    string[] strArr = RecordId.Split('_');
                    if (String.IsNullOrEmpty(strArr[0]))
                    {
                    }
                }
                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "Delete from [dbo].[GroupRuleDetail] where [GroupRuleId]=@GroupRuleId \n";
                    cmd.CommandText += "Delete from [dbo].[GroupRule] where [GroupRuleId]=@GroupRuleId \n";
                    cmd.Parameters.AddWithValue("@GroupRuleId", RecordId);
                    cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    objResultSet.Message = "Success";
                    objResultSet.Result = true;
                }
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objResultSet.Message = excObj.Message;
                objResultSet.Result = false;
            }
            return objResultSet;
        }

        public ResultSet SaveOrderofNodesWithinGroup(string NodeOrder)
        {
            ResultSet objResultSet = new ResultSet();
            try
            {
                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }
                string[] strArrNodes = NodeOrder.Split('_');
                for (int i = 0; i < strArrNodes.Length; i++)
                {
                    string strNode = Convert.ToString(strArrNodes[i]);
                    string[] strArrNodeAndIndex = strNode.Split(':');

                    if (strArrNodeAndIndex.Length < 2)
                        continue;
                    using (SqlCommand cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = "Update [dbo].[GroupRule] set [Order]=@Order where [GroupRuleId]=@GroupRuleId \n";
                        cmd.Parameters.AddWithValue("@GroupRuleId", Convert.ToString(strArrNodeAndIndex[0]));
                        cmd.Parameters.AddWithValue("@Order", Convert.ToString(strArrNodeAndIndex[1]));
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();

                        objResultSet.Message = "Success";
                        objResultSet.Result = true;
                    }
                }
            }
            catch (Exception excObj)
            {
                if (_connection.State != ConnectionState.Closed)
                {
                    _connection.Close();
                }
                objResultSet.Message = excObj.Message;
                objResultSet.Result = false;
            }
            return objResultSet;
        }

        #endregion

        #region Auto Merge Rule

        #region Save Entity Auto Merge Rule

        public EntityAutoMergeRuleResultSet SaveEntityAutoMergeRule(Guid autoMergeId, Guid entitySettingId, bool status = true)
        {
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                EntityAutoMergeRule objEntityAutoMergeRule = new EntityAutoMergeRule();
                objEntityAutoMergeRule.EntityRuleId = Guid.NewGuid();
                objEntityAutoMergeRule.AutoMergeRuleId = autoMergeId;
                objEntityAutoMergeRule.EntitySettingId = entitySettingId;
                objEntityAutoMergeRule.Status = status;
                objPAM2EntitiesContext.EntityAutoMergeRules.Add(objEntityAutoMergeRule);
                objPAM2EntitiesContext.SaveChanges();

                return new EntityAutoMergeRuleResultSet()
                {
                    Message = "Success",
                    Result = true
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Update Entity Auto Merge Rule

        public EntityAutoMergeRuleResultSet UpdateEntityAutoMergeRule(Guid entityRuleId, bool status = true)
        {
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                EntityAutoMergeRule objEntityAutoMergeRule = objPAM2EntitiesContext.EntityAutoMergeRules.Where(p => p.EntityRuleId == entityRuleId).SingleOrDefault();
                objEntityAutoMergeRule.Status = status;
                objPAM2EntitiesContext.SaveChanges();
                return new EntityAutoMergeRuleResultSet()
                {
                    Message = "Success",
                    Result = true
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Get All Entity Auto Merge Rule

        public EntityAutoMergeRuleResultSet getAllEntityAutoMergeRule(Guid entitySettingId)
        {
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                var lstEntityAutoMergeRule = objPAM2EntitiesContext.EntityAutoMergeRules.Where(p => p.EntitySettingId == entitySettingId).Join(objPAM2EntitiesContext.AutoMergeRuleMasters,
                    EAMR => EAMR.AutoMergeRuleId, AMR => AMR.AutoMergeRuleId,
                    (EAMR, AMR) =>
                        new
                        {
                            EntityRuleId = EAMR.EntityRuleId,
                            EntitySettingId = EAMR.EntitySettingId,
                            AutoMergeRuleId = EAMR.AutoMergeRuleId,
                            Description = AMR.Description,
                            Status = EAMR.Status
                        }
                    ).AsEnumerable().Select(p => new EntityAutoMergeRule
                    {
                        EntityRuleId = p.EntityRuleId,
                        EntitySettingId = p.EntitySettingId,
                        AutoMergeRuleId = p.AutoMergeRuleId,
                        Description = p.Description,
                        Status = p.Status
                    }).ToList<EntityAutoMergeRule>();
                return new EntityAutoMergeRuleResultSet()
                {
                    Message = "Success",
                    Result = true,
                    EntityAutoMergeRules = lstEntityAutoMergeRule
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #endregion

        #region Entity  Section

        public Section AddEntitySection(PAMSectionAttributeSetting sectionDetails, Guid pamUserId)
        {
            Section addedSection = null;
            Section foundSection = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (sectionDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(sectionDetails.EntitySettingId) && !string.IsNullOrEmpty(sectionDetails.SectionId))
                {
                    Guid entitySettingIdGUID = new Guid(sectionDetails.EntitySettingId);
                    Guid sectionIdGuid = new Guid(sectionDetails.SectionId);
                    foundSection = pam2EntitiesContext.Sections.Where(section => section.EntitySettingId == entitySettingIdGUID && section.SectionId == sectionIdGuid).FirstOrDefault();
                }
                if (foundSection != null)
                {
                    int displyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sectionDetails.DisplayOrder))
                    {
                        Int32.TryParse(sectionDetails.DisplayOrder, out displyOrder);
                    }

                    foundSection.DisplayOrder = displyOrder;
                    foundSection.SectionName = sectionDetails.GroupName;
                    foundSection.UpdateDate = DateTime.UtcNow;
                    //this need to be added after integration with CRM
                    foundSection.UpdatedBy = pamUserId;
                    pam2EntitiesContext.SaveChanges();
                    addedSection = foundSection;
                }
                else
                {
                    Section newSection = new Section();
                    newSection.SectionName = sectionDetails.GroupName;
                    int displyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sectionDetails.DisplayOrder))
                    {
                        Int32.TryParse(sectionDetails.DisplayOrder, out displyOrder);
                    }
                    newSection.DisplayOrder = displyOrder;
                    newSection.EntitySettingId = new Guid(sectionDetails.EntitySettingId);
                    //this field need to be taken from extjs and will be sent across request and applied
                    newSection.CreatedBy = pamUserId;
                    newSection.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.Sections.Add(newSection);
                    pam2EntitiesContext.SaveChanges();
                    addedSection = newSection;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSection;
        }

        public ResultSet DeleteEntitySection(string SectionId)
        {
            ResultSet sectionResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Section section = null;
                Guid sectionIdGuid = new Guid(SectionId);
                section = pam2EntitiesContext.Sections.Where(s => s.SectionId == sectionIdGuid).FirstOrDefault();
                if (section != null)
                {
                    section = pam2EntitiesContext.Sections.Remove(section);
                    int count = pam2EntitiesContext.SaveChanges();
                    sectionResult.Message = "Success";
                    sectionResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionResult;
        }

        #endregion

        #region Session Section

        public Section AddSessionSection(PAMMatchGroupAttributeSetting sectionDetails, Guid pamUserId, string sessionId)
        {
            Section addedSection = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Section newSection = new Section();
                newSection.SectionName = sectionDetails.GroupName;
                int displyOrder = 0;
                if (!string.IsNullOrWhiteSpace(sectionDetails.DisplayOrder))
                {
                    Int32.TryParse(sectionDetails.DisplayOrder, out displyOrder);
                }
                if (sectionDetails.GroupName.ToLower().Equals("default"))
                {
                    displyOrder = 999;
                }
                newSection.DisplayOrder = displyOrder;
                newSection.EntitySettingId = new Guid(sectionDetails.EntitySettingId);
                //this field need to be taken from extjs and will be sent across request and applied
                newSection.CreatedBy = pamUserId;
                newSection.CreatedDate = DateTime.UtcNow;
                pam2EntitiesContext.Sections.Add(newSection);
                pam2EntitiesContext.SaveChanges();
                addedSection = newSection;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSection;
        }

        public Section AddSessionSectionForEdit(PAMMatchGroupAttributeSetting sectionDetails, Guid pamUserId, string sessionId)
        {
            Section addedSection = null;
            Section foundSection = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (!string.IsNullOrWhiteSpace(sessionId) && sectionDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(sectionDetails.EntitySettingId) && !string.IsNullOrEmpty(sectionDetails.SectionId))
                {
                    Guid entitySettingIdGUID = new Guid(sectionDetails.EntitySettingId);
                    Guid sectionIdGuid = new Guid(sectionDetails.SectionId);
                    Guid sessionIdGuid = new Guid(sessionId);
                    foundSection = pam2EntitiesContext.Sections.Where(section => section.EntitySettingId == entitySettingIdGUID && section.SectionId == sectionIdGuid).FirstOrDefault();
                }
                if (foundSection != null)
                {
                    int displyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sectionDetails.DisplayOrder))
                    {
                        Int32.TryParse(sectionDetails.DisplayOrder, out displyOrder);
                    }
                    if (foundSection.SectionName.ToLower().Equals("default"))
                    {
                        displyOrder = 999;
                    }
                    foundSection.DisplayOrder = displyOrder;
                    foundSection.SectionName = sectionDetails.GroupName;
                    foundSection.UpdateDate = DateTime.UtcNow;
                    //this need to be added after integration with CRM
                    foundSection.UpdatedBy = pamUserId;
                    pam2EntitiesContext.SaveChanges();
                    addedSection = foundSection;
                }
                else
                {
                    Section newSection = new Section();
                    newSection.SectionName = sectionDetails.GroupName;
                    int displyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sectionDetails.DisplayOrder))
                    {
                        Int32.TryParse(sectionDetails.DisplayOrder, out displyOrder);
                    }
                    if (sectionDetails.GroupName.ToLower().Equals("default"))
                    {
                        displyOrder = 999;
                    }
                    newSection.DisplayOrder = displyOrder;
                    newSection.EntitySettingId = new Guid(sectionDetails.EntitySettingId);
                    //this field need to be taken from extjs and will be sent across request and applied
                    newSection.CreatedBy = pamUserId;
                    newSection.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.Sections.Add(newSection);
                    pam2EntitiesContext.SaveChanges();
                    addedSection = newSection;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSection;
        }

        public SessionSection AddSessionSectionTableEntry(PAMMatchGroupAttributeSetting sectionDetails, Guid pamUserId, string sessionId)
        {
            SessionSection addedSection = null;
            SessionSection foundSection = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (!string.IsNullOrWhiteSpace(sessionId) && sectionDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(sectionDetails.EntitySettingId) && !string.IsNullOrEmpty(sectionDetails.SectionId))
                {
                    Guid entitySettingIdGUID = new Guid(sectionDetails.EntitySettingId);
                    Guid sectionIdGuid = new Guid(sectionDetails.SectionId);
                    Guid sessionIdGuid = new Guid(sessionId);
                    foundSection = pam2EntitiesContext.SessionSections.Where(section => section.SectionId == sectionIdGuid && section.SessionId == sessionIdGuid).FirstOrDefault();
                }
                if (foundSection != null)
                {
                    int displyOrder = 0;
                    if (!string.IsNullOrWhiteSpace(sectionDetails.DisplayOrder))
                    {
                        Int32.TryParse(sectionDetails.DisplayOrder, out displyOrder);
                    }
                    foundSection.SectionId = new Guid(sectionDetails.SectionId);
                    foundSection.SessionId = new Guid(sessionId);
                    foundSection.UpdatedDate = DateTime.UtcNow;
                    //this need to be added after integration with CRM
                    foundSection.UpdatedBy = pamUserId;
                    pam2EntitiesContext.SaveChanges();
                    addedSection = foundSection;
                }
                else
                {
                    SessionSection newSection = new SessionSection();
                    newSection.SectionId = new Guid(sectionDetails.SectionId);
                    newSection.SessionId = new Guid(sessionId);
                    //this field need to be taken from extjs and will be sent across request and applied
                    newSection.CreatedBy = pamUserId;
                    newSection.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SessionSections.Add(newSection);
                    pam2EntitiesContext.SaveChanges();
                    addedSection = newSection;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSection;
        }

        public ResultSet DeleteSessionSection(string SectionId, string sessionId)
        {
            ResultSet sectionResult = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid sectionIdGuid = new Guid(SectionId);
                SessionSection Ssection = pam2EntitiesContext.SessionSections.Where(s => s.SessionId == new Guid(sessionId) && s.SectionId == sectionIdGuid).FirstOrDefault();
                if (Ssection != null)
                {
                    Ssection = pam2EntitiesContext.SessionSections.Remove(Ssection);
                    int count = pam2EntitiesContext.SaveChanges();
                    Section section = null;
                    section = pam2EntitiesContext.Sections.Where(s => s.SectionId == sectionIdGuid).FirstOrDefault();
                    if (section != null)
                    {
                        section = pam2EntitiesContext.Sections.Remove(section);
                        count = pam2EntitiesContext.SaveChanges();
                    }
                    sectionResult.Message = "Success";
                    sectionResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionResult;
        }

        #endregion

        #region Section Attribute Setting

        public AttributeSetting AddEntityDisplaySettings(PAMSectionAttributeSetting attributeDetails, Guid pamUserId)
        {
            AttributeSetting addedAttributeSetting = null;
            AttributeSetting foundAttributeSetting = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (attributeDetails.MatchAttributeSettingId != null && !string.IsNullOrWhiteSpace(attributeDetails.MatchAttributeSettingId) && attributeDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(attributeDetails.EntitySettingId) && !string.IsNullOrEmpty(attributeDetails.SectionId))
                {
                    Guid entitySettingIdGUID = new Guid(attributeDetails.EntitySettingId);
                    Guid sectionIdGuid = new Guid(attributeDetails.SectionId);
                    Guid attributeSettingIdGuid = new Guid(attributeDetails.MatchAttributeSettingId);
                    foundAttributeSetting = pam2EntitiesContext.AttributeSettings.Where(attributeSetting => attributeSetting.AttributeSettingId == attributeSettingIdGuid && attributeSetting.EntitySettingId == entitySettingIdGUID && attributeSetting.SectionId == sectionIdGuid && attributeSetting.CustomName != "header").FirstOrDefault();
                }
                if (foundAttributeSetting != null)
                {
                    foundAttributeSetting.DisplayName = attributeDetails.DisplayName;
                    foundAttributeSetting.UpdateDate = DateTime.UtcNow;
                    foundAttributeSetting.CustomName = attributeDetails.CustomName;
                    int displayOrder = 0;
                    if (!string.IsNullOrWhiteSpace(attributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(attributeDetails.DisplayOrder, out displayOrder);
                    }
                    foundAttributeSetting.DisplayOrder = displayOrder;
                    foundAttributeSetting.ExcludeUpdate = attributeDetails.ExcludeUpdate;
                    foundAttributeSetting.IsVisible = attributeDetails.IsVisible;
                    foundAttributeSetting.SectionId = new Guid(attributeDetails.SectionId);
                    // this needs to be implemented when integrated with CRM
                    foundAttributeSetting.UpdateBy = pamUserId;
                    foundAttributeSetting.EntitySettingId = new Guid(attributeDetails.EntitySettingId);
                    foundAttributeSetting.SchemaName = attributeDetails.SchemaName;
                    foundAttributeSetting.UseForAutoMerge = attributeDetails.UseForAutoMerge;
                    foundAttributeSetting.SessionId = null;
                    pam2EntitiesContext.SaveChanges();
                    addedAttributeSetting = foundAttributeSetting;
                }
                else
                {
                    AttributeSetting newAttributeSetting = new AttributeSetting();
                    newAttributeSetting.DisplayName = attributeDetails.DisplayName;
                    newAttributeSetting.CustomName = attributeDetails.CustomName;
                    int displayOrder = 0;
                    if (!string.IsNullOrWhiteSpace(attributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(attributeDetails.DisplayOrder, out displayOrder);
                    }
                    newAttributeSetting.DisplayOrder = displayOrder;
                    newAttributeSetting.ExcludeUpdate = attributeDetails.ExcludeUpdate;
                    newAttributeSetting.IsVisible = attributeDetails.IsVisible;
                    newAttributeSetting.SectionId = new Guid(attributeDetails.SectionId);
                    newAttributeSetting.EntitySettingId = new Guid(attributeDetails.EntitySettingId);
                    newAttributeSetting.SchemaName = attributeDetails.SchemaName;
                    newAttributeSetting.SessionId = null;
                    newAttributeSetting.UseForAutoMerge = attributeDetails.UseForAutoMerge;
                    //this field need to be taken from extjs and will be sent across request and applied
                    newAttributeSetting.CreatedBy = pamUserId;
                    newAttributeSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.AttributeSettings.Add(newAttributeSetting);
                    pam2EntitiesContext.SaveChanges();
                    addedAttributeSetting = newAttributeSetting;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedAttributeSetting;
        }

        // Following is an old method to fetch sectionAttributes from database and to bind the grid on Display settings
        public MatchGroupAttributeSettingResultSet GetSectionsAttributeSettings(string entitySettingId)
        {
            MatchGroupAttributeSettingResultSet sectionAttributeSettingResultSet = new MatchGroupAttributeSettingResultSet();
            Guid entitySettingIdGUID = new Guid(entitySettingId);
            List<PAMMatchGroupAttributeSetting> lstSectionAttributeSetting = new List<PAMMatchGroupAttributeSetting>();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<Section> lstSections = (from s in pam2EntitiesContext.Sections
                                             join ss in pam2EntitiesContext.SessionSections on s.SectionId equals ss.SectionId
                                             into resultSec
                                             from newSec in resultSec.DefaultIfEmpty()
                                             where newSec == null
                                             select s).ToList<Section>();
                var lstResult = (from s in lstSections
                                 join a in pam2EntitiesContext.AttributeSettings.OrderBy(c => c.DisplayOrder) on s.SectionId equals a.SectionId
                                 into result
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby s.DisplayOrder, index
                                 where s.EntitySettingId == entitySettingIdGUID && (AttrSetting == null || (AttrSetting.SessionId == null && AttrSetting.CustomName.ToLower() != "header" && AttrSetting.SectionId != null))
                                 select new { Section = s, AttrSetting }
                                 ).Distinct().ToList();
                PAMMatchGroupAttributeSetting parentNode = new PAMMatchGroupAttributeSetting();
                Guid previousSectiond = Guid.Empty;
                List<PAMMatchGroupAttributeSetting> lstChildren = new List<PAMMatchGroupAttributeSetting>();
                foreach (var obj in lstResult)
                {
                    if (obj == null)
                        continue;
                    PAMMatchGroupAttributeSetting childNode = new PAMMatchGroupAttributeSetting();
                    if (previousSectiond == Guid.Empty || (obj.Section.SectionId != null && obj.Section.SectionId != Guid.Empty && lstSectionAttributeSetting.Find(s => s.SectionId == obj.Section.SectionId.ToString()) == null))
                    {
                        parentNode = new PAMMatchGroupAttributeSetting();
                        parentNode.children = new List<PAMMatchGroupAttributeSetting>();
                        lstChildren = new List<PAMMatchGroupAttributeSetting>();
                        parentNode.leaf = false;
                        parentNode.text = obj.Section.SectionName;
                        parentNode.cls = "folder";
                        parentNode.EntitySettingId = obj.Section.EntitySettingId.ToString();
                        parentNode.GroupName = obj.Section.SectionName;
                        parentNode.id = obj.Section.SectionId.ToString();
                        //parentCount += 1;
                        //parentNode.MatchAttributeSettingId = parentCount.ToString();
                        //  parentNode.MatchGroupId = obj.SectionId.ToString();
                        parentNode.SectionId = obj.Section.SectionId.ToString();
                        parentNode.qtip = obj.Section.SectionName;
                        parentNode.DisplayName = obj.Section.SectionName;
                        parentNode.SchemaName = "";
                        parentNode.DisplayOrder = Convert.ToString(obj.Section.DisplayOrder);
                        lstSectionAttributeSetting.Add(parentNode);
                    }
                    if (obj.AttrSetting != null && obj.AttrSetting.AttributeSettingId != null && obj.AttrSetting.AttributeSettingId != Guid.Empty && obj.AttrSetting.CustomName.ToLower() != "header")
                    {
                        childNode.leaf = true;
                        childNode.text = obj.AttrSetting.DisplayName;
                        childNode.cls = "file";
                        childNode.EntitySettingId = obj.AttrSetting.EntitySettingId.ToString();
                        childNode.GroupName = obj.Section.SectionName;
                        childNode.id = obj.AttrSetting.AttributeSettingId.ToString();
                        childNode.MatchAttributeSettingId = obj.AttrSetting.AttributeSettingId.ToString();
                        //     childNode.MatchGroupId = obj.SectionId.ToString();
                        childNode.SectionId = obj.AttrSetting.SectionId.ToString();
                        childNode.qtip = obj.AttrSetting.DisplayName;
                        childNode.SchemaName = obj.AttrSetting.SchemaName;
                        childNode.DisplayName = obj.AttrSetting.DisplayName;
                        childNode.CustomName = obj.AttrSetting.CustomName;
                        childNode.ExcludeUpdate = obj.AttrSetting.ExcludeUpdate;
                        childNode.DisplayOrder = Convert.ToString(obj.AttrSetting.DisplayOrder);
                        childNode.UseForAutoMerge = obj.AttrSetting.UseForAutoMerge;
                        lstChildren.Add(childNode);
                        parentNode.children = lstChildren;
                    }
                    previousSectiond = obj.Section.SectionId;
                }
                sectionAttributeSettingResultSet.Message = "Success";
                sectionAttributeSettingResultSet.MatchGroupAttributeSettings = lstSectionAttributeSetting;
                sectionAttributeSettingResultSet.Result = true;
                sectionAttributeSettingResultSet.success = true;
                sectionAttributeSettingResultSet.total = lstSectionAttributeSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionAttributeSettingResultSet;
        }

        // Following is the new method to fetch sectionAttributes (with rules) from database and to bind the grids on Display settings as best field detection feature is added on this page
        public PAMSectionAttributeSettingResultSet GetSectionsAttributeSettingsWithRules(string entitySettingId, bool FromSession=false)
        {
            PAMSectionAttributeSettingResultSet sectionAttributeSettingResultSet = new PAMSectionAttributeSettingResultSet();

            Guid entitySettingIdGUID = new Guid(entitySettingId);
            List<PAMSectionAttributeSetting> lstSectionAttributeSetting = new List<PAMSectionAttributeSetting>();

            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettingsRoot = new List<PAMBestFieldDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<Section> lstSections = (from s in pam2EntitiesContext.Sections
                                             join ss in pam2EntitiesContext.SessionSections on s.SectionId equals ss.SectionId
                                             into resultSec
                                             from newSec in resultSec.DefaultIfEmpty()
                                             where newSec == null
                                             select s).ToList<Section>();
                var lstResult = (from s in lstSections
                                 join a in pam2EntitiesContext.AttributeSettings.OrderBy(c => c.DisplayOrder) on s.SectionId equals a.SectionId
                                 into result
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby s.DisplayOrder, index
                                 where s.EntitySettingId == entitySettingIdGUID && (AttrSetting == null || (AttrSetting.SessionId == null && AttrSetting.CustomName.ToLower() != "header" && AttrSetting.SectionId != null))
                                 select new { Section = s, AttrSetting }
                                 ).Distinct().ToList();
                PAMSectionAttributeSetting parentNode = new PAMSectionAttributeSetting();
                Guid previousSectiond = Guid.Empty;

                List<PAMSectionAttributeSetting> lstChildren = new List<PAMSectionAttributeSetting>();

                List<PAMBestFieldDetectionSettings> lstBestFieldDetectionSetting = GetBestFieldDetectionSettings().BestFieldDetectionSettings;
                                                                                //(from s in pam2EntitiesContext.BestFieldDetectionSettings
                                                                                //select s).ToList<BestFieldDetectionSetting>();

                List<PAMBestFieldDetectionSettings> lstBestFieldDetectionSettingSectionFiltered = (from c in lstBestFieldDetectionSetting
                                                                                               join d in lstSections on c.SectionId equals d.SectionId
                                                                                                   select c).ToList<PAMBestFieldDetectionSettings>();

                var lstResultNew =  lstResult.Where(c => c.AttrSetting != null).ToList();
                List<PAMBestFieldDetectionSettings> lstBestFieldDetectionSettingFieldsFiltered = (from c in lstBestFieldDetectionSetting
                                                                                                  join d in lstResultNew
                                                                                                  on 
                                                                                              c.AttributeSettingId equals d.AttrSetting.AttributeSettingId 
                                                                                               where d.AttrSetting != null
                                                                                               select c).ToList<PAMBestFieldDetectionSettings>();

                foreach (var obj in lstResult)
                {
                    if (obj == null)
                        continue;

                    PAMSectionAttributeSetting childNode = new PAMSectionAttributeSetting();

                    if (previousSectiond == Guid.Empty || (obj.Section.SectionId != null && obj.Section.SectionId != Guid.Empty && lstSectionAttributeSetting.Find(s => s.SectionId == obj.Section.SectionId.ToString()) == null))
                    {
                        parentNode = new PAMSectionAttributeSetting();
                        parentNode.children = new List<PAMSectionAttributeSetting>();
                        lstChildren = new List<PAMSectionAttributeSetting>();
                        parentNode.leaf = false;
                        parentNode.text = obj.Section.SectionName;
                        parentNode.cls = "folder";
                        parentNode.EntitySettingId = obj.Section.EntitySettingId.ToString();
                        parentNode.GroupName = obj.Section.SectionName;
                        parentNode.id = obj.Section.SectionId.ToString();
                        //parentCount += 1;
                        //parentNode.MatchAttributeSettingId = parentCount.ToString();
                        //  parentNode.MatchGroupId = obj.SectionId.ToString();
                        parentNode.SectionId = obj.Section.SectionId.ToString();

                        // use the DataType field to save true/false whether the records from default display settings or created in new in session
                        if(FromSession)
                            parentNode.DataType = "true"; 
                        List<PAMBestFieldDetectionSettings> lstTempBestFieldDetectionSetting = lstBestFieldDetectionSettingSectionFiltered.Where(c => c.SectionId == new Guid(parentNode.SectionId)).ToList<PAMBestFieldDetectionSettings>();

                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettingsRoot = new PAMBestFieldDetectionSettings();
                        objPAMBestFieldDetectionSettingsRoot.leaf = false;
                        objPAMBestFieldDetectionSettingsRoot.text = "Rules Sequence";
                        objPAMBestFieldDetectionSettingsRoot.qtip = "Rules Sequence";
                        objPAMBestFieldDetectionSettingsRoot.RuleName = "Rules Sequence";
                        objPAMBestFieldDetectionSettingsRoot.id = "1";
                        objPAMBestFieldDetectionSettingsRoot.cls = "folder";
                        objPAMBestFieldDetectionSettingsRoot.Id = Convert.ToString(Guid.NewGuid());
                        objPAMBestFieldDetectionSettingsRoot.children = lstTempBestFieldDetectionSetting;
                        lstPAMBestFieldDetectionSettingsRoot = new List<PAMBestFieldDetectionSettings>();
                        lstPAMBestFieldDetectionSettingsRoot.Add(objPAMBestFieldDetectionSettingsRoot);

                        parentNode.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettingsRoot;
                        
                        parentNode.qtip = obj.Section.SectionName;
                        parentNode.DisplayName = obj.Section.SectionName;
                        parentNode.SchemaName = "";
                        parentNode.DisplayOrder = Convert.ToString(obj.Section.DisplayOrder);
                        lstSectionAttributeSetting.Add(parentNode);
                    }
                    if (obj.AttrSetting != null && obj.AttrSetting.AttributeSettingId != null && obj.AttrSetting.AttributeSettingId != Guid.Empty && obj.AttrSetting.CustomName.ToLower() != "header")
                    {
                        childNode.leaf = true;
                        childNode.text = obj.AttrSetting.DisplayName;
                        childNode.cls = "file";
                        childNode.EntitySettingId = obj.AttrSetting.EntitySettingId.ToString();
                        childNode.GroupName = obj.Section.SectionName;
                        childNode.id = obj.AttrSetting.AttributeSettingId.ToString();
                        // use the DataType field to save true/false whether the records from default display settings or created in new in session
                        if (FromSession)
                            childNode.DataType = "true"; 

                        List<PAMBestFieldDetectionSettings> lstTempBestFieldDetectionSetting = lstBestFieldDetectionSettingFieldsFiltered.Where(c => c.AttributeSettingId == new Guid(childNode.id)).ToList<PAMBestFieldDetectionSettings>();

                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettingsRoot = new PAMBestFieldDetectionSettings();
                        objPAMBestFieldDetectionSettingsRoot.leaf = false;
                        objPAMBestFieldDetectionSettingsRoot.text = "Rules Sequence";
                        objPAMBestFieldDetectionSettingsRoot.qtip = "Rules Sequence";
                        objPAMBestFieldDetectionSettingsRoot.RuleName = "Rules Sequence";
                        objPAMBestFieldDetectionSettingsRoot.id = "1";
                        objPAMBestFieldDetectionSettingsRoot.cls = "folder";
                        objPAMBestFieldDetectionSettingsRoot.Id = Convert.ToString(Guid.NewGuid());
                        objPAMBestFieldDetectionSettingsRoot.children = lstTempBestFieldDetectionSetting;
                        lstPAMBestFieldDetectionSettingsRoot = new List<PAMBestFieldDetectionSettings>();
                        lstPAMBestFieldDetectionSettingsRoot.Add(objPAMBestFieldDetectionSettingsRoot);

                        childNode.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettingsRoot;


                        childNode.MatchAttributeSettingId = obj.AttrSetting.AttributeSettingId.ToString();
                        //     childNode.MatchGroupId = obj.SectionId.ToString();
                        childNode.SectionId = obj.AttrSetting.SectionId.ToString();
                        childNode.qtip = obj.AttrSetting.DisplayName;
                        childNode.SchemaName = obj.AttrSetting.SchemaName;
                        childNode.DisplayName = obj.AttrSetting.DisplayName;
                        childNode.IsVisible = obj.AttrSetting.IsVisible;
                        childNode.CustomName = obj.AttrSetting.CustomName;
                        childNode.ExcludeUpdate = obj.AttrSetting.ExcludeUpdate;
                        childNode.DisplayOrder = Convert.ToString(obj.AttrSetting.DisplayOrder);
                        childNode.UseForAutoMerge = obj.AttrSetting.UseForAutoMerge;
                        lstChildren.Add(childNode);
                        parentNode.children = lstChildren;
                    }
                    previousSectiond = obj.Section.SectionId;
                }
                sectionAttributeSettingResultSet.Message = "Success";
                sectionAttributeSettingResultSet.SectionAttributeSettings = lstSectionAttributeSetting;
                sectionAttributeSettingResultSet.Result = true;
                sectionAttributeSettingResultSet.success = true;
                sectionAttributeSettingResultSet.total = lstSectionAttributeSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionAttributeSettingResultSet;
        }

        public ResultSet DeleteEntityDisplayAttributeSetting(string AttributeSettingId)
        {
            var attributeSettingResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                AttributeSetting attributeSetting = null;
                Guid attributeSettingIdGuid = new Guid(AttributeSettingId);

                List<BestFieldDetectionSetting> tobeDeletedSettings = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                                       where c.AttributeSettingId == attributeSettingIdGuid
                                                                       select c).ToList<BestFieldDetectionSetting>();

                if (tobeDeletedSettings.Count > 0)
                {
                    List<BestFieldDetPicklistFieldDetail> lstBestFieldDetPicklistFieldDetail = (from e in pam2EntitiesContext.BestFieldDetPicklistFieldDetails
                                                                                                join c in pam2EntitiesContext.BestFieldDetectionSettings on e.BestFieldDetectionSettingsId
                                                                                                equals c.Id
                                                                                                where c.AttributeSettingId == attributeSettingIdGuid
                                                                                                select e).ToList<BestFieldDetPicklistFieldDetail>();
                    if (lstBestFieldDetPicklistFieldDetail.Count > 0)
                    {
                        pam2EntitiesContext.BestFieldDetPicklistFieldDetails.RemoveRange(lstBestFieldDetPicklistFieldDetail);
                        pam2EntitiesContext.SaveChanges();
                    }

                    pam2EntitiesContext.BestFieldDetectionSettings.RemoveRange(tobeDeletedSettings);
                    pam2EntitiesContext.SaveChanges();
                }

                attributeSetting = pam2EntitiesContext.AttributeSettings.Where(s => s.AttributeSettingId == attributeSettingIdGuid).FirstOrDefault();
                if (attributeSetting != null)
                {
                    attributeSetting = pam2EntitiesContext.AttributeSettings.Remove(attributeSetting);
                    int count = pam2EntitiesContext.SaveChanges();
                    attributeSettingResult.Message = "Success";
                    attributeSettingResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return attributeSettingResult;
        }

        public ResultSet DeleteAllEntityDisplayAttributeSetting(Guid SectionId)
        {
            var attributeSettingResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<AttributeSetting> attributeSettingList = null;
                attributeSettingList = pam2EntitiesContext.AttributeSettings.Where(s => s.SectionId == SectionId).ToList();

                List<BestFieldDetectionSetting> tobeDeletedSettings = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                                       join d in pam2EntitiesContext.AttributeSettings on c.AttributeSettingId
                                                                       equals d.AttributeSettingId
                                                                       where d.SectionId == SectionId
                                                                       select c).ToList<BestFieldDetectionSetting>();
          
                if (tobeDeletedSettings.Count > 0)
                {
                    List<BestFieldDetPicklistFieldDetail> lstBestFieldDetPicklistFieldDetail = (from e in pam2EntitiesContext.BestFieldDetPicklistFieldDetails
                                                                                                join c in pam2EntitiesContext.BestFieldDetectionSettings on e.BestFieldDetectionSettingsId
                                                                                                equals c.Id
                                                                                                join d in pam2EntitiesContext.AttributeSettings on c.AttributeSettingId
                                                                                                equals d.AttributeSettingId
                                                                                                where d.SectionId == SectionId
                                                                                                select e).ToList<BestFieldDetPicklistFieldDetail>();
                    if (lstBestFieldDetPicklistFieldDetail.Count > 0)
                    {
                        pam2EntitiesContext.BestFieldDetPicklistFieldDetails.RemoveRange(lstBestFieldDetPicklistFieldDetail);
                        pam2EntitiesContext.SaveChanges();
                    }

                    pam2EntitiesContext.BestFieldDetectionSettings.RemoveRange(tobeDeletedSettings);
                    pam2EntitiesContext.SaveChanges();
                }

                if (attributeSettingList != null && attributeSettingList.Count > 0)
                {
                    foreach (var attribute in attributeSettingList)
                    {
                        pam2EntitiesContext.AttributeSettings.Remove(attribute);
                        int count = pam2EntitiesContext.SaveChanges();
                    }
                }
                attributeSettingResult.Message = "Success";
                attributeSettingResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return attributeSettingResult;
        }

        public AttributeSetting AddEntityRecordHeaderDisplaySetting(PAMMatchGroupAttributeSetting attributeDetails, Guid pamUserId)
        {
            AttributeSetting addedAttributeSetting = null;
            AttributeSetting foundAttributeSetting = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (attributeDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(attributeDetails.EntitySettingId))
                {
                    Guid entitySettingIdGUID = new Guid(attributeDetails.EntitySettingId);
                    foundAttributeSetting = pam2EntitiesContext.AttributeSettings.Where(attributeSetting => attributeSetting.EntitySettingId == entitySettingIdGUID && attributeSetting.CustomName == "header").FirstOrDefault();
                }
                if (foundAttributeSetting != null)
                {
                    foundAttributeSetting.DisplayName = attributeDetails.DisplayName;
                    foundAttributeSetting.UpdateDate = DateTime.UtcNow;
                    foundAttributeSetting.CustomName = "header";
                    foundAttributeSetting.DisplayOrder = 0;
                    foundAttributeSetting.SectionId = null;
                    // this needs to be implemented when integrated with CRM
                    foundAttributeSetting.UpdateBy = pamUserId;
                    foundAttributeSetting.EntitySettingId = new Guid(attributeDetails.EntitySettingId);
                    foundAttributeSetting.SchemaName = attributeDetails.SchemaName;
                    foundAttributeSetting.SessionId = null;
                    pam2EntitiesContext.SaveChanges();
                    addedAttributeSetting = foundAttributeSetting;
                }
                else
                {
                    AttributeSetting newAttributeSetting = new AttributeSetting();
                    newAttributeSetting.DisplayName = attributeDetails.DisplayName;
                    newAttributeSetting.CustomName = "header";
                    newAttributeSetting.SectionId = null;
                    newAttributeSetting.EntitySettingId = new Guid(attributeDetails.EntitySettingId);
                    newAttributeSetting.SchemaName = attributeDetails.SchemaName;
                    newAttributeSetting.SessionId = null;
                    //this field need to be taken from extjs and will be sent across request and applied
                    newAttributeSetting.CreatedBy = pamUserId;
                    newAttributeSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.AttributeSettings.Add(newAttributeSetting);
                    pam2EntitiesContext.SaveChanges();
                    addedAttributeSetting = newAttributeSetting;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedAttributeSetting;
        }

        public AttributeSetting GetEntityRecordHeaderDisplaySetting(string entitySettingId)
        {
            AttributeSetting foundAttributeSetting = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (!string.IsNullOrWhiteSpace(entitySettingId))
                {
                    Guid entitySettingIdGUID = new Guid(entitySettingId);
                    foundAttributeSetting = pam2EntitiesContext.AttributeSettings.Where(attributeSetting => attributeSetting.EntitySettingId == entitySettingIdGUID && attributeSetting.CustomName == "Header").FirstOrDefault();
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return foundAttributeSetting;
        }

        #endregion

        #region Session GroupRule

        public PAMGroupRuleResultSet GetGroupRulesForSession(List<PAMMatchGroupAttributeSetting> MatchGroupIDs, string SessionID)
        {
            PAMGroupRuleResultSet objGroupRuleResultSet = new PAMGroupRuleResultSet();
            SqlDataReader dr = null;
            List<PAMGroupRule> lstGroupRule = new List<PAMGroupRule>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<MatchGroupMaster> lstMatchGroupMaster = pam2EntitiesContext.MatchGroupMasters.ToList<MatchGroupMaster>();

                if (String.IsNullOrEmpty(SessionID))
                {
                    List<MatchGroupMaster> lstMatchGroupMasterFiltered = lstMatchGroupMaster.Where(p => MatchGroupIDs.Any(i => i.DisplayName.ToLower().Trim() == p.Name.ToLower().Trim().ToString())).ToList<MatchGroupMaster>();
                    string strMatchGroupMasterIDs = String.Join(",", lstMatchGroupMasterFiltered.Select(i => i.MatchGroupId));
                    strMatchGroupMasterIDs = "('" + strMatchGroupMasterIDs.Replace(",", "','") + "')";
                    if (_connection == null)
                    {
                        _connection = new SqlConnection(sqlConnString);
                    }

                    if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                    {
                        _connection.Open();
                    }

                    using (SqlCommand cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = @"Select GroupRuleId, MGM.MatchGroupId, MGM.Name as GroupName, GR.RuleId, MRM.RuleName, MRM.Enum, GR.Description
                                          from
                                          dbo.MatchGroupMaster MGM left join dbo.GroupRule GR on GR.GroupId=MGM.MatchGroupId
                                          left join dbo.MatchRuleMaster MRM on GR.RuleId = MRM.MatchRuleId
                                          where cast(MGM.MatchGroupId as nvarchar(50)) in " + strMatchGroupMasterIDs +
                                          " order by MGM.Name, GR.[Order]";
                        dr = cmd.ExecuteReader();
                        PAMGroupRule GroupRuleObj = new PAMGroupRule();
                        List<PAMGroupRule> lstMatchRule = new List<PAMGroupRule>();
                        string strPreviousGroup = String.Empty;
                        int i = 1;
                        while (dr.Read())
                        {
                            PAMGroupRule objMatchRule = new PAMGroupRule();
                            if (String.IsNullOrEmpty(strPreviousGroup) || String.Compare(strPreviousGroup, Convert.ToString(dr["MatchGroupId"])) != 0)
                            {
                                if (!String.IsNullOrEmpty(strPreviousGroup))
                                {
                                    GroupRuleObj.leaf = false;
                                    lstGroupRule.Add(GroupRuleObj);
                                    GroupRuleObj = new PAMGroupRule();
                                    GroupRuleObj.children = new List<PAMGroupRule>();
                                }

                                lstMatchRule = new List<PAMGroupRule>();
                                GroupRuleObj.id = i++.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                                GroupRuleObj.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                                GroupRuleObj.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                                //  GroupRuleObj.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);
                                GroupRuleObj.text = Convert.ToString(dr["GroupName"]);
                                GroupRuleObj.qtip = Convert.ToString(dr["GroupName"]);
                                GroupRuleObj.cls = "folder";
                            }

                            if (Convert.ToString(dr["RuleId"]) != String.Empty)
                            {
                                objMatchRule.id = Convert.ToString(dr["GroupRuleId"]);
                                objMatchRule.MatchRuleId = Convert.ToString(dr["RuleId"]);
                                objMatchRule.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                                objMatchRule.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                                objMatchRule.text = Convert.ToString(dr["RuleName"]);
                                objMatchRule.RuleName = Convert.ToString(dr["RuleName"]);
                                objMatchRule.Enum = Convert.ToString(dr["Enum"]);

                                if (objMatchRule.Enum == DataEnums.Rules.CustomTransformLibrary.ToString())
                                {
                                    GroupRuleDetail obj = GetGropRuleDetailByGroupRuleId(Convert.ToString(dr["GroupRuleId"]));
                                    if (obj != null)
                                        objMatchRule.id = Convert.ToString(dr["GroupRuleId"]) + "_" + obj.AttributeValue;
                                }

                                objMatchRule.Description = Convert.ToString(dr["Description"]);
                                objMatchRule.qtip = Convert.ToString(dr["Description"]);
                                objMatchRule.cls = "file";
                                objMatchRule.leaf = true;

                                if (objMatchRule.Description.ToUpper().Contains("NAME") && objMatchRule.Enum.ToLower() == "extractname")
                                    objMatchRule.Description = objMatchRule.Description.Replace("NAME", " NAME");

                                //if (objMatchRule.Description.ToLower().Contains("personalname") && objMatchRule.Enum.ToLower() == "normalise")
                                //    objMatchRule.Description = objMatchRule.Description.Replace("personalname", "First Names");

                                //if (objMatchRule.Description.ToLower().Contains("otherchars") && objMatchRule.Enum.ToLower() == "removechars")
                                //    objMatchRule.Description = objMatchRule.Description.Replace("otherchars", "Other Characters");

                                //if (objMatchRule.Description.ToLower().Contains("rbd") && objMatchRule.Enum.ToLower() == "customexclude")
                                //    objMatchRule.Description = objMatchRule.Description.Replace("rbd", "Remove Between Delimiters");

                                //if (objMatchRule.Description.ToLower().Contains("rdo") && objMatchRule.Enum.ToLower() == "customexclude")
                                //    objMatchRule.Description = objMatchRule.Description.Replace("rdo", "Remove Delimiters Only");

                                //if (objMatchRule.Description.ToLower().Contains("rdab") && objMatchRule.Enum.ToLower() == "customexclude")
                                //    objMatchRule.Description = objMatchRule.Description.Replace("rdab", "Remove Delimiters and Between");

                                //if (objMatchRule.Description.ToLower().Contains("rod") && objMatchRule.Enum.ToLower() == "customexclude")
                                //    objMatchRule.Description = objMatchRule.Description.Replace("rod", "Remove Outside the Delimiters");

                                lstMatchRule.Add(objMatchRule);
                                GroupRuleObj.children = lstMatchRule;
                            }

                            strPreviousGroup = Convert.ToString(dr["MatchGroupId"]);
                        }

                        if (!String.IsNullOrEmpty(GroupRuleObj.id))
                        {
                            GroupRuleObj.leaf = false;
                            lstGroupRule.Add(GroupRuleObj);
                        }
                    }

                    dr.Close();
                    if (_connection.State != ConnectionState.Closed)
                    {
                        _connection.Close();
                    }
                    // Get the match groups not in matchgroup master -- created new on match group screen
                    List<PAMMatchGroupAttributeSetting> lstPAMMatchGroupAttributeSetting = (from list in MatchGroupIDs
                                                                                            join mg in pam2EntitiesContext.MatchGroupMasters on list.DisplayName.ToLower().Trim() equals mg.Name.ToLower().Trim().ToString()
                                                                                            into newlist
                                                                                            from n in newlist.DefaultIfEmpty()
                                                                                            where n == null
                                                                                            select list).ToList<PAMMatchGroupAttributeSetting>();

                    foreach (PAMMatchGroupAttributeSetting objPAMMatchGroupAttributeSetting in lstPAMMatchGroupAttributeSetting)
                    {
                        PAMGroupRule GroupRuleObj1 = new PAMGroupRule();
                        GroupRuleObj1.leaf = false;
                        GroupRuleObj1.children = new List<PAMGroupRule>();
                        object objId = lstGroupRule.Max(c => c.id);
                        if (objId != null)
                        {
                            objId = Convert.ToInt32(objId) + 1;
                        }
                        else
                            objId = 1;

                        GroupRuleObj1.id = objId.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                        GroupRuleObj1.GroupRuleId = null;
                        GroupRuleObj1.MatchGroupId = objId.ToString();
                        GroupRuleObj1.IsMaster = false;
                        GroupRuleObj1.text = objPAMMatchGroupAttributeSetting.GroupName;
                        GroupRuleObj1.qtip = objPAMMatchGroupAttributeSetting.GroupName;
                        GroupRuleObj1.cls = "folder";
                        lstGroupRule.Add(GroupRuleObj1);
                    }

                    objGroupRuleResultSet.Message = "Success";
                    objGroupRuleResultSet.GroupRules = lstGroupRule;
                    objGroupRuleResultSet.Result = true;
                }
                else
                {
                    //// if session id is not null or empty i.e. When Clone
                    // First get the rule of the groups same as original group
                    Guid gSessionID = new Guid(SessionID);
                    List<PAMMatchGroupAttributeSetting> lstPAMMatchGroupAttributeSetting = (from list in MatchGroupIDs
                                                                                            join sg in pam2EntitiesContext.SessionGroups on list.MatchGroupId.ToLower().Trim() equals sg.MatchGroupId.ToString().ToLower().Trim()
                                                                                            where sg.SessionId == gSessionID
                                                                                            select list).ToList<PAMMatchGroupAttributeSetting>();

                    List<PAMMatchGroupAttributeSetting> lstRemainingGRoups = new List<PAMMatchGroupAttributeSetting>(MatchGroupIDs);
                    foreach (PAMMatchGroupAttributeSetting objPAMMatchGroupAttributeSetting in lstPAMMatchGroupAttributeSetting)
                    {
                        if (lstRemainingGRoups.Contains(objPAMMatchGroupAttributeSetting))
                            lstRemainingGRoups.Remove(objPAMMatchGroupAttributeSetting);

                        PAMGroupRule GroupRuleObj1 = new PAMGroupRule();
                        GroupRuleObj1.leaf = false;

                        GroupRuleObj1.children = new List<PAMGroupRule>();

                        GroupRuleObj1.children = ((SessionID != string.Empty && objPAMMatchGroupAttributeSetting.MatchGroupId != string.Empty) ? pam2EntitiesContext.GroupRules.Where(p => p.GroupId == new Guid(objPAMMatchGroupAttributeSetting.MatchGroupId)).Join(pam2EntitiesContext.MatchRuleMasters, GR => GR.RuleId, MRM => MRM.MatchRuleId, (GR, MRM) => new { GR = GR, MRM = MRM }).AsEnumerable().OrderBy(p => p.GR.Order).Select(p => new PAMGroupRule()
                        {
                            id = Convert.ToString(p.GR.GroupRuleId),
                            MatchRuleId = Convert.ToString(p.GR.RuleId),
                            GroupRuleId = Convert.ToString(p.GR.GroupRuleId),
                            MatchGroupId = Convert.ToString(p.GR.GroupId),
                            //  objMatchRule.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);
                            text = Convert.ToString(p.MRM.RuleName),
                            RuleName = Convert.ToString(p.MRM.RuleName),
                            Enum = Convert.ToString(p.MRM.Enum),
                            Description = Convert.ToString(p.GR.Description),
                            qtip = Convert.ToString(p.GR.Description),
                            cls = "file",
                            leaf = true
                        }).ToList<PAMGroupRule>() : new List<PAMGroupRule>());

                        foreach (PAMGroupRule objGroupRule in GroupRuleObj1.children)
                        {
                            if (objGroupRule.Description.Contains("NAME") && objGroupRule.Enum.ToLower() == "extractname")
                                objGroupRule.Description = objGroupRule.Description.Replace("NAME", " NAME");
                        }

                        object objId = lstGroupRule.Max(c => c.id);
                        if (objId != null)
                        {
                            objId = Convert.ToInt32(objId) + 1;
                        }
                        else
                            objId = 1;

                        GroupRuleObj1.id = objId.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                        GroupRuleObj1.GroupRuleId = null;
                        GroupRuleObj1.MatchGroupId = objPAMMatchGroupAttributeSetting.MatchGroupId;
                        GroupRuleObj1.IsMaster = false;
                        GroupRuleObj1.text = objPAMMatchGroupAttributeSetting.GroupName;
                        GroupRuleObj1.qtip = objPAMMatchGroupAttributeSetting.GroupName;
                        GroupRuleObj1.cls = "folder";
                        lstGroupRule.Add(GroupRuleObj1);
                    }
                    // Second : get rules from the remaining groups which are same as MatchGroupmaster
                    List<MatchGroupMaster> lstMatchGroupMasterFiltered = lstMatchGroupMaster.Where(p => lstRemainingGRoups.Any(i => i.DisplayName.ToLower().Trim() == p.Name.ToLower().Trim().ToString())).ToList<MatchGroupMaster>();
                    if (lstMatchGroupMasterFiltered.Count() != 0)
                    {
                        string strMatchGroupMasterIDs = String.Join(",", lstMatchGroupMasterFiltered.Select(i => i.MatchGroupId));
                        strMatchGroupMasterIDs = "('" + strMatchGroupMasterIDs.Replace(",", "','") + "')";

                        if (_connection == null)
                        {
                            _connection = new SqlConnection(sqlConnString);
                        }

                        if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                        {
                            _connection.Open();
                        }

                        using (SqlCommand cmd = _connection.CreateCommand())
                        {
                            cmd.CommandText = @"Select GroupRuleId, MGM.MatchGroupId, MGM.Name as GroupName, GR.RuleId, MRM.RuleName, MRM.Enum, GR.Description
                                          from
                                          dbo.MatchGroupMaster MGM left join dbo.GroupRule GR on GR.GroupId=MGM.MatchGroupId
                                          left join dbo.MatchRuleMaster MRM on GR.RuleId = MRM.MatchRuleId
                                          where cast(MGM.MatchGroupId as nvarchar(50)) in " + strMatchGroupMasterIDs +
                                              " order by MGM.Name, GR.[Order]";
                            dr = cmd.ExecuteReader();

                            PAMGroupRule GroupRuleObj = new PAMGroupRule();
                            List<PAMGroupRule> lstMatchRule = new List<PAMGroupRule>();
                            string strPreviousGroup = String.Empty;
                            object objId = lstGroupRule.Max(c => c.id);
                            if (objId != null)
                            {
                                objId = Convert.ToInt32(objId) + 1;
                            }
                            else
                                objId = 1;

                            int i = (int)objId;

                            while (dr.Read())
                            {
                                string strMatchGroupName = Convert.ToString(dr["GroupName"]).ToLower().Trim();
                                lstRemainingGRoups.Remove(lstRemainingGRoups.Where(c => c.GroupName.Trim().ToLower() == strMatchGroupName.Trim().ToLower()).FirstOrDefault());
                                PAMGroupRule objMatchRule = new PAMGroupRule();
                                if (String.IsNullOrEmpty(strPreviousGroup) || String.Compare(strPreviousGroup, Convert.ToString(dr["MatchGroupId"])) != 0)
                                {
                                    if (!String.IsNullOrEmpty(strPreviousGroup))
                                    {
                                        GroupRuleObj.leaf = false;
                                        lstGroupRule.Add(GroupRuleObj);
                                        GroupRuleObj = new PAMGroupRule();
                                        GroupRuleObj.children = new List<PAMGroupRule>();
                                    }

                                    lstMatchRule = new List<PAMGroupRule>();
                                    GroupRuleObj.id = i++.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                                    GroupRuleObj.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                                    GroupRuleObj.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                                    //  GroupRuleObj.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);
                                    GroupRuleObj.text = Convert.ToString(dr["GroupName"]);
                                    GroupRuleObj.qtip = Convert.ToString(dr["GroupName"]);
                                    GroupRuleObj.cls = "folder";
                                }

                                if (Convert.ToString(dr["RuleId"]) != String.Empty)
                                {
                                    objMatchRule.id = Convert.ToString(dr["GroupRuleId"]);
                                    objMatchRule.MatchRuleId = Convert.ToString(dr["RuleId"]);
                                    objMatchRule.GroupRuleId = Convert.ToString(dr["GroupRuleId"]);
                                    objMatchRule.MatchGroupId = Convert.ToString(dr["MatchGroupId"]);
                                    objMatchRule.text = Convert.ToString(dr["RuleName"]);
                                    objMatchRule.RuleName = Convert.ToString(dr["RuleName"]);
                                    objMatchRule.Enum = Convert.ToString(dr["Enum"]);
                                    objMatchRule.Description = Convert.ToString(dr["Description"]);

                                    if (objMatchRule.Description.Contains("NAME") && objMatchRule.Enum.ToLower() == "extractname")
                                        objMatchRule.Description = objMatchRule.Description.Replace("NAME", " NAME");

                                    objMatchRule.qtip = Convert.ToString(dr["Description"]);
                                    objMatchRule.cls = "file";
                                    objMatchRule.leaf = true;
                                    lstMatchRule.Add(objMatchRule);
                                    GroupRuleObj.children = lstMatchRule;
                                }
                                strPreviousGroup = Convert.ToString(dr["MatchGroupId"]);
                            }

                            if (!String.IsNullOrEmpty(GroupRuleObj.id))
                            {
                                GroupRuleObj.leaf = false;
                                lstGroupRule.Add(GroupRuleObj);
                            }
                        }

                        dr.Close();

                        if (_connection.State != ConnectionState.Closed)
                        {
                            _connection.Close();
                        }
                    }

                    // Third : Get the rules which are newly created in the new session
                    foreach (PAMMatchGroupAttributeSetting objPAMMatchGroupAttributeSetting in lstRemainingGRoups)
                    {
                        PAMGroupRule GroupRuleObj1 = new PAMGroupRule();
                        GroupRuleObj1.leaf = false;
                        GroupRuleObj1.children = new List<PAMGroupRule>();
                        //GroupRuleObj1.children = ((SessionID != string.Empty && objPAMMatchGroupAttributeSetting.MatchGroupId != string.Empty) ? pam2EntitiesContext.GroupRules.Where(p => p.GroupId == new Guid(objPAMMatchGroupAttributeSetting.MatchGroupId)).Join(pam2EntitiesContext.MatchRuleMasters, GR => GR.RuleId, MRM => MRM.MatchRuleId, (GR, MRM) => new { GR = GR, MRM = MRM }).AsEnumerable().Select(p => new PAMGroupRule()
                        //{
                        //    id = Convert.ToString(p.GR.GroupRuleId),
                        //    MatchRuleId = Convert.ToString(p.GR.RuleId),
                        //    GroupRuleId = Convert.ToString(p.GR.GroupRuleId),
                        //    MatchGroupId = Convert.ToString(p.GR.GroupId),
                        //    //  objMatchRule.IsMaster = dr["IsMaster"] == DBNull.Value ? false : Convert.ToBoolean(dr["IsMaster"]);

                        //    text = Convert.ToString(p.MRM.RuleName),
                        //    RuleName = Convert.ToString(p.MRM.RuleName),
                        //    Enum = Convert.ToString(p.MRM.Enum),
                        //    Description = Convert.ToString(p.MRM.Description),
                        //    qtip = Convert.ToString(p.MRM.Description),
                        //    cls = "file",
                        //    leaf = true
                        //}).ToList<PAMGroupRule>() : new List<PAMGroupRule>());

                        object objId = lstGroupRule.Max(c => c.id);
                        if (objId != null)
                        {
                            objId = Convert.ToInt32(objId) + 1;
                        }
                        else
                            objId = 1;

                        GroupRuleObj1.id = objId.ToString(); // Convert.ToString(dr["GroupRuleId"]) + "_" + Convert.ToString(dr["MatchGroupId"]);
                        GroupRuleObj1.GroupRuleId = null;
                        GroupRuleObj1.MatchGroupId = objId.ToString();
                        GroupRuleObj1.IsMaster = false;
                        GroupRuleObj1.text = objPAMMatchGroupAttributeSetting.GroupName;
                        GroupRuleObj1.qtip = objPAMMatchGroupAttributeSetting.GroupName;
                        GroupRuleObj1.cls = "folder";
                        lstGroupRule.Add(GroupRuleObj1);
                    }

                    objGroupRuleResultSet.Message = "Success";
                    objGroupRuleResultSet.GroupRules = lstGroupRule;
                    objGroupRuleResultSet.Result = true;

                    foreach (PAMGroupRule objPAMGroupRule in objGroupRuleResultSet.GroupRules)
                    {
                        foreach (PAMGroupRule objPAMGroupRule1 in objPAMGroupRule.children)
                        {
                            if (objPAMGroupRule1.Enum == DataEnums.Rules.CustomTransformLibrary.ToString())
                            {
                                GroupRuleDetail obj = GetGropRuleDetailByGroupRuleId(objPAMGroupRule1.GroupRuleId);
                                if (obj != null)
                                    objPAMGroupRule1.id = objPAMGroupRule1.id + "_" + obj.AttributeValue;
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objGroupRuleResultSet;
        }

        public ResultSet SaveSessionGroupRules(List<PAMGroupRule> GroupRules, string SessionID)
        {
            ResultSet objResultSet = new ResultSet();
            try
            {
                Guid SessionGuid = new Guid(SessionID);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                // Get already created match groups
                List<MatchGroup> lstMatchGroup = pam2EntitiesContext.MatchGroups.Where(p => p.IsMaster == false).ToList<MatchGroup>();
                List<MatchGroup> lstMatchGroupFiltered = lstMatchGroup.Where(p => GroupRules.Any(i => i.text.ToLower().Trim() == p.DisplayName.ToLower().Trim()) && p.SessionGroups.Any(j => j.SessionId == SessionGuid)).ToList<MatchGroup>();

                //List<SessionGroup> lstSessionGroups = pam2EntitiesContext.SessionGroups.Where(p => p.SessionId == SessionGuid).ToList<SessionGroup>();
                //List<SessionGroup> SessionGroups = lstSessionGroups.Where(p => GroupRules.Any(i => i.text.ToLower().Trim() == p.MatchGroup.DisplayName.ToLower().Trim())).ToList<SessionGroup>();
                //List<GroupRule> lstGroupRules = (from c in pam2EntitiesContext.GroupRules
                //                                 join
                //                                    d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                //                                 where d.SessionId == SessionGuid
                //                                 select c).ToList<GroupRule>();

                //List<GroupRuleDetail> lstGroupRuleDetail = (from e in pam2EntitiesContext.GroupRuleDetails
                //                                            join c in pam2EntitiesContext.GroupRules on e.GroupRuleId equals c.GroupRuleId
                //                                            join d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                //                                            where d.SessionId == SessionGuid
                //                                            select e).ToList<GroupRuleDetail>();

                // pam2EntitiesContext.SaveChanges();
                if (_connection == null)
                {
                    _connection = new SqlConnection(sqlConnString);
                }

                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (SqlCommand cmd = _connection.CreateCommand())
                {
                    foreach (var objGroupRule in GroupRules)
                    {
                        int iOrder = 0;
                        //string GroupName = "";
                        MatchGroup objMatchGroup = lstMatchGroupFiltered.Where(p => p.DisplayName.ToLower().Trim() == objGroupRule.text.Trim().ToLower()).FirstOrDefault();
                        //if (objMatchGroup != null)
                        //    GroupName = objMatchGroup.DisplayName;
                        foreach (var objGrouprule1 in objGroupRule.children)
                        {
                            Guid GroupRuleId = Guid.NewGuid();
                            cmd.CommandText = @"INSERT INTO [dbo].[GroupRule] ([GroupRuleId],[GroupId],[RuleId],[IsMaster],[Order],[Description]) VALUES (@GroupRuleId,@GroupId,@RuleId,0,@Order,@Description)";
                            cmd.Parameters.AddWithValue("@GroupRuleId", GroupRuleId);
                            cmd.Parameters.AddWithValue("@GroupId", objMatchGroup.MatchGroupId);
                            cmd.Parameters.AddWithValue("@RuleId", objGrouprule1.MatchRuleId);
                            cmd.Parameters.AddWithValue("@Order", iOrder);
                            cmd.Parameters.AddWithValue("@Description", objGrouprule1.Description);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                            string strDescription = objGrouprule1.Description;
                            ArrayList arrAttr = new ArrayList();
                            ArrayList arrAttrValues = new ArrayList();
                            switch (objGrouprule1.Enum)
                            {
                                case "CustomExclude":
                                    // @@WholeWords (@@LookFor->@@ChangeTo)
                                    arrAttr.Add(DataEnums.RuleAttributes.LeftDelimiter.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RightDelimiter.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.Mode.ToString());

                                    if (strDescription.Contains("Remove Between Delimiters"))
                                        strDescription = strDescription.Replace("Remove Between Delimiters", "RBD");

                                    if (strDescription.Contains("Remove Delimiters Only"))
                                        strDescription = strDescription.Replace("Remove Delimiters Only", "RDO");

                                    if (strDescription.Contains("Remove Delimiters and Between"))
                                        strDescription = strDescription.Replace("Remove Delimiters and Between", "RDAB");

                                    if (strDescription.Contains("Remove Outside the Delimiters"))
                                        strDescription = strDescription.Replace("Remove Outside the Delimiters", "ROD");

                                    int iIndex = strDescription.IndexOf("(");
                                    string strDescriptionData = strDescription.Substring(iIndex, strDescription.Length - iIndex);
                                    string strMode = strDescription.Replace(strDescriptionData, String.Empty);
                                    strDescriptionData = strDescriptionData.Replace("('", String.Empty).Replace("')", String.Empty);
                                    strDescriptionData = strDescriptionData.Replace("'->'", ">");
                                    //  char[] charParam = new char[] { '-', '>' };
                                    string[] strData = strDescriptionData.Split('>');
                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                        arrAttrValues.Add(strMode);
                                    }

                                    break;

                                case "CustomTransform":
                                    // @@WholeWords (@@LookFor->@@ChangeTo)
                                    arrAttr.Add(DataEnums.RuleAttributes.LookFor.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.ChangeTo.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.WholeWords.ToString());

                                    iIndex = strDescription.IndexOf("(");
                                    strDescriptionData = strDescription.Substring(iIndex, strDescription.Length - iIndex);
                                    string strWholeWords = strDescription.Replace(strDescriptionData, String.Empty).Trim();
                                    if (String.IsNullOrEmpty(strWholeWords))
                                    {
                                        strWholeWords = "0";
                                    }
                                    else
                                    {
                                        strWholeWords = "1";
                                    }

                                    strDescriptionData = strDescriptionData.Replace("(", String.Empty).Replace(")", String.Empty);
                                    strDescriptionData = strDescriptionData.Replace("-", String.Empty);
                                    // charParam = new char[] { '-', '>' };
                                    strData = strDescriptionData.Split('>');
                                    if (strData.Length > 1)
                                    {
                                        string strLookFor = strData[0].ToString();
                                        strLookFor = strLookFor.Substring(1, strLookFor.Length - 2);
                                        arrAttrValues.Add(strLookFor);

                                        string strChangeTo = strData[1].ToString();
                                        strChangeTo = strChangeTo.Substring(1, strChangeTo.Length - 2);
                                        arrAttrValues.Add(strChangeTo);

                                        arrAttrValues.Add(strWholeWords);
                                    }

                                    break;

                                case "ExtractLetters":
                                    // @@ELDirection @@ELNumber letters
                                    arrAttr.Add(DataEnums.RuleAttributes.ELDirection.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.ELNumber.ToString());

                                    strDescription = strDescription.Replace("Letter(s)", String.Empty);
                                    strData = strDescription.Split(' ');

                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                    }

                                    break;

                                case "ExtractWord":
                                    // @@EWDirection @@EWNumber words
                                    arrAttr.Add(DataEnums.RuleAttributes.EWDirection.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.EWNumber.ToString());

                                    strDescription = strDescription.Replace("Word(s)", String.Empty);
                                    strData = strDescription.Split(' ');

                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());
                                        arrAttrValues.Add(strData[1].ToString());
                                    }

                                    break;

                                case "ExtractName":
                                    // @@ExtractName
                                    arrAttr.Add(DataEnums.RuleAttributes.ExtractName.ToString());
                                    if (strDescription.ToUpper().Contains("FIRST NAME") || strDescription.ToUpper().Contains("MIDDLE NAME") || strDescription.ToUpper().Contains("LAST NAME"))
                                    {
                                        strDescription = strDescription.Replace(" name", "name");
                                    }

                                    if (strDescription.ToUpper().Contains("SUFFIX OR QUALIFICATION"))
                                    {
                                        strDescription = "SUFFIX";
                                    }

                                    if (strDescription.ToUpper().Contains("PREFIX OR TITLE"))
                                    {
                                        strDescription = "PREFIX";
                                    }

                                    arrAttrValues.Add(strDescription);
                                    break;

                                case "RemoveChars":
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvVowels.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvConsonants.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvNumbers.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvPunctuation.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.RemvOtherChars.ToString());

                                    if (strDescription.Contains("Other Characters"))
                                    {
                                        strDescription = strDescription.Replace("Other Characters", "OtherChars");
                                    }

                                    for (int i = 0; i < arrAttr.Count; i++)
                                    {
                                        string strAttr = Convert.ToString(arrAttr[i]).Replace("Remv", String.Empty);
                                        if (strDescription.Contains(strAttr))
                                            arrAttrValues.Add(true);
                                        else
                                            arrAttrValues.Add(false);
                                    }

                                    break;

                                case "Normalise":
                                    arrAttr.Add(DataEnums.RuleAttributes.Method.ToString());
                                    arrAttr.Add(DataEnums.RuleAttributes.Category.ToString());

                                    if (strDescription.Contains("First Names"))
                                    {
                                        strDescription = strDescription.Replace("First Names", "PersonalName");
                                    }

                                    if (strDescription.Contains("Job Titles"))
                                    {
                                        strDescription = strDescription.Replace("Job Titles", "BusinessJobTitle");
                                    }

                                    if (strDescription.Contains("Dates and Events"))
                                    {
                                        strDescription = strDescription.Replace("Dates and Events", "Dates");
                                    }

                                    if (strDescription.Contains("Weights and Measures"))
                                    {
                                        strDescription = strDescription.Replace("Weights and Measures", "WeightsMeasures");
                                    }

                                    strData = strDescription.Split(' ');

                                    if (strData.Length > 1)
                                    {
                                        arrAttrValues.Add(strData[0].ToString());

                                        arrAttrValues.Add(strData[1].ToString());
                                    }

                                    break;

                                case "CustomTransformLibrary":
                                    arrAttr.Add(DataEnums.RuleAttributes.CTLCategory.ToString());
                                    string id = objGrouprule1.id;
                                    string[] strArr = id.Split('_');
                                    string strCategoryId = String.Empty;

                                    if (strArr.Length > 1)
                                    {
                                        strCategoryId = strArr[1].ToString();
                                    }

                                    CategoryMaster objCategoryMaster = InsertCTLCategoryForSession(strCategoryId, SessionID);
                                    arrAttrValues.Add(objCategoryMaster.CategoryId.ToString());
                                    break;

                            }

                            // Insert in GroupRuleDetail table
                            for (int i = 0; i < arrAttr.Count; i++)
                            {
                                cmd.CommandText = @"INSERT INTO [dbo].[GroupRuleDetail] ([GroupRuleDetailId],[GroupRuleId],[AttributeEnum],[AttributeValue]) 
                                        VALUES (@GroupRuleDetailId,@GroupRuleId,@AttributeEnum,@AttributeValue)";
                                cmd.Parameters.AddWithValue("@GroupRuleDetailId", Guid.NewGuid());
                                cmd.Parameters.AddWithValue("@GroupRuleId", GroupRuleId);
                                cmd.Parameters.AddWithValue("@AttributeEnum", Convert.ToString(arrAttr[i]));
                                cmd.Parameters.AddWithValue("@AttributeValue", Convert.ToString(arrAttrValues[i]));
                                cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();
                            }

                            iOrder++;
                        }
                    }
                }
                objResultSet.success = true;
            }
            catch (Exception ex)
            {
                objResultSet.success = false;
                objResultSet.Message = ex.ToString();
            }
            return objResultSet;
        }

        public CategoryMaster InsertCTLCategoryForSession(string CategoryId, string sessionID)
        {
            CategoryMaster objCategoryMasterNew = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid gCategoryId = new Guid(CategoryId);
                CategoryMaster objCategoryMaster = pam2EntitiesContext.CategoryMasters.Where(c => c.CategoryId == gCategoryId).FirstOrDefault();
                List<CategoryDetail> lstCategoryDetail = pam2EntitiesContext.CategoryDetails.Where(c => c.CategoryId == gCategoryId).ToList<CategoryDetail>();

                if (objCategoryMaster != null)
                {
                    AddCTLCategory(objCategoryMaster.Category, false, objCategoryMaster.CreatedBy.ToString(), out objCategoryMasterNew);
                }

                foreach (CategoryDetail objCategoryDetail in lstCategoryDetail)
                {
                    AddCTLCategoryDetail(objCategoryMasterNew.CategoryId.ToString(), objCategoryDetail.FromText, objCategoryDetail.ToText, objCategoryDetail.CreatedBy.ToString());
                }

                //public ResultSet AddCTLCategory(string Category, bool IsMaster, string CreatedBy)

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objCategoryMasterNew;
        }

        public ResultSet DeleteSessionGroupRule(string sessionId)
        {
            ResultSet result = new ResultSet();
            try
            {
                Guid sessionGuid = new Guid(sessionId);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<GroupRule> lstGroupRules = (from c in pam2EntitiesContext.GroupRules
                                                 join
                                                    d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                                                 where d.SessionId == sessionGuid
                                                 select c).ToList<GroupRule>();

                List<GroupRuleDetail> lstGroupRuleDetail = (from e in pam2EntitiesContext.GroupRuleDetails
                                                            join c in pam2EntitiesContext.GroupRules on e.GroupRuleId equals c.GroupRuleId
                                                            join d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                                                            where d.SessionId == sessionGuid
                                                            select e).ToList<GroupRuleDetail>();

                foreach (var item in lstGroupRuleDetail)
                {
                    if (item.GroupRule.MatchRuleMaster.Enum == DataEnums.Rules.CustomTransformLibrary.ToString())
                    {
                        DeleteCTLCategory(item.AttributeValue.ToString());
                    }

                    pam2EntitiesContext.GroupRuleDetails.Remove(item);

                }

                foreach (var item in lstGroupRules)
                {
                    pam2EntitiesContext.GroupRules.Remove(item);
                }

                pam2EntitiesContext.SaveChanges();
                result.Result = true;
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                throw ex;
            }
            return result;
        }

        public ResultSet DeleteMatchGroupRule(string MatchGroupId)
        {
            ResultSet result = new ResultSet();
            try
            {
                Guid MatchGroupIdguid = new Guid(MatchGroupId);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<GroupRule> lstGroupRules = (from c in pam2EntitiesContext.GroupRules
                                                 join
                                                    d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                                                 where d.MatchGroupId == MatchGroupIdguid
                                                 select c).ToList<GroupRule>();

                List<GroupRuleDetail> lstGroupRuleDetail = (from e in pam2EntitiesContext.GroupRuleDetails
                                                            join c in pam2EntitiesContext.GroupRules on e.GroupRuleId equals c.GroupRuleId
                                                            join d in pam2EntitiesContext.SessionGroups on c.GroupId equals d.MatchGroupId
                                                            where d.MatchGroupId == MatchGroupIdguid
                                                            select e).ToList<GroupRuleDetail>();

                foreach (var item in lstGroupRuleDetail)
                {
                    pam2EntitiesContext.GroupRuleDetails.Remove(item);
                }
                foreach (var item in lstGroupRules)
                {
                    pam2EntitiesContext.GroupRules.Remove(item);
                }

                pam2EntitiesContext.SaveChanges();
                result.Result = true;
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                throw ex;
            }
            return result;
        }
        #endregion

        #region Session Section Attribute Setting
        // Following is an old method to fetch sessionsectionAttributes from database and to bind the grid on Display settings
        public MatchGroupAttributeSettingResultSet GetSessionSectionsAttributeSettings(string entitySettingId, string sessionId)
        {
            MatchGroupAttributeSettingResultSet sectionAttributeSettingResultSet = new MatchGroupAttributeSettingResultSet();
            List<PAMMatchGroupAttributeSetting> lstSectionAttributeSetting = new List<PAMMatchGroupAttributeSetting>();

            try
            {

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    sectionAttributeSettingResultSet = GetDefaultSessionSectionsAttributeSettings(entitySettingId);
                    return sectionAttributeSettingResultSet;
                }

                Guid entitySettingIdGUID = new Guid(entitySettingId);
                Guid sessionIdGUID = new Guid(sessionId);

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                var lstResult = (from ss in pam2EntitiesContext.SessionSections
                                 join s in pam2EntitiesContext.Sections on ss.SectionId equals s.SectionId
                                 join a in pam2EntitiesContext.AttributeSettings on s.SectionId equals a.SectionId
                                    into result
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby s.DisplayOrder, index
                                 where ss.SessionId == sessionIdGUID && s.EntitySettingId == entitySettingIdGUID
                                 select new { Section = s, AttrSetting }
                                    ).ToList();


                PAMMatchGroupAttributeSetting parentNode = new PAMMatchGroupAttributeSetting();
                Guid previousSectiond = Guid.Empty;
                List<PAMMatchGroupAttributeSetting> lstChildren = new List<PAMMatchGroupAttributeSetting>();

                if (lstResult != null && lstResult.Count > 0)
                {
                    foreach (var obj in lstResult)
                    {
                        if (obj == null)
                            continue;
                        PAMMatchGroupAttributeSetting childNode = new PAMMatchGroupAttributeSetting();

                        if (previousSectiond == Guid.Empty || (obj.Section.SectionId != null && obj.Section.SectionId != Guid.Empty && lstSectionAttributeSetting.Find(s => s.SectionId == obj.Section.SectionId.ToString()) == null))
                        {
                            parentNode = new PAMMatchGroupAttributeSetting();
                            parentNode.children = new List<PAMMatchGroupAttributeSetting>();
                            lstChildren = new List<PAMMatchGroupAttributeSetting>();
                            parentNode.leaf = false;
                            parentNode.text = obj.Section.SectionName;
                            parentNode.cls = "folder";
                            parentNode.EntitySettingId = obj.Section.EntitySettingId.ToString();
                            parentNode.GroupName = obj.Section.SectionName;
                            parentNode.id = obj.Section.SectionId.ToString();
                            //parentCount += 1;
                            //parentNode.MatchAttributeSettingId = parentCount.ToString();
                            //  parentNode.MatchGroupId = obj.SectionId.ToString();
                            parentNode.SectionId = obj.Section.SectionId.ToString();
                            parentNode.qtip = obj.Section.SectionName;
                            parentNode.DisplayName = obj.Section.SectionName;
                            parentNode.SchemaName = "";
                            parentNode.DisplayOrder = Convert.ToString(obj.Section.DisplayOrder);
                            parentNode.SessionId = sessionId;
                            lstSectionAttributeSetting.Add(parentNode);
                        }

                        if (obj.AttrSetting != null && obj.AttrSetting.AttributeSettingId != Guid.Empty && obj.AttrSetting.CustomName.ToLower() != "header")
                        {
                            childNode.leaf = true;
                            childNode.text = obj.AttrSetting.DisplayName;
                            childNode.cls = "file";
                            childNode.EntitySettingId = obj.AttrSetting.EntitySettingId.ToString();
                            childNode.GroupName = obj.Section.SectionName;
                            childNode.id = obj.AttrSetting.AttributeSettingId.ToString();
                            childNode.MatchAttributeSettingId = obj.AttrSetting.AttributeSettingId.ToString();
                            //     childNode.MatchGroupId = obj.SectionId.ToString();
                            childNode.SectionId = obj.AttrSetting.SectionId.ToString();
                            childNode.qtip = obj.AttrSetting.DisplayName;
                            childNode.SchemaName = obj.AttrSetting.SchemaName;
                            childNode.DisplayName = obj.AttrSetting.DisplayName;
                            childNode.IsVisible = obj.AttrSetting.IsVisible;
                            childNode.CustomName = obj.AttrSetting.CustomName;
                            childNode.ExcludeUpdate = obj.AttrSetting.ExcludeUpdate;
                            childNode.DisplayOrder = Convert.ToString(obj.AttrSetting.DisplayOrder);
                            childNode.SessionId = sessionId;
                            lstChildren.Add(childNode);
                            parentNode.children = lstChildren;
                        }

                        previousSectiond = obj.Section.SectionId;
                    }

                    sectionAttributeSettingResultSet.Message = "Success";
                    sectionAttributeSettingResultSet.MatchGroupAttributeSettings = lstSectionAttributeSetting.Distinct().OrderBy(p => p.DisplayOrder).ToList<PAMMatchGroupAttributeSetting>();
                    sectionAttributeSettingResultSet.Result = true;
                    sectionAttributeSettingResultSet.success = true;
                    sectionAttributeSettingResultSet.total = lstSectionAttributeSetting.Count;

                }
                else
                {
                    sectionAttributeSettingResultSet = GetDefaultSessionSectionsAttributeSettings(entitySettingId);
                    return sectionAttributeSettingResultSet;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionAttributeSettingResultSet;
        }

        // Following is a new method to fetch sessionsectionAttributes from database and to bind the grid on Display settings as best field detection feature is added on this page.
        public PAMSectionAttributeSettingResultSet GetSessionSectionsAttributeSettingsWithRules(string entitySettingId, string sessionId)
        {
            PAMSectionAttributeSettingResultSet sectionAttributeSettingResultSet = new PAMSectionAttributeSettingResultSet();
            List<PAMSectionAttributeSetting> lstSectionAttributeSetting = new List<PAMSectionAttributeSetting>();

            try
            {

              //  if (string.IsNullOrWhiteSpace(sessionId))
                {
                //  sectionAttributeSettingResultSet = GetDefaultSessionSectionsAttributeSettings(entitySettingId);
                    sectionAttributeSettingResultSet = GetSectionsAttributeSettingsWithRules(entitySettingId, true);
                    if (string.IsNullOrWhiteSpace(sessionId))
                         return sectionAttributeSettingResultSet;
                }

                List<PAMSectionAttributeSetting> lstSectionAttributeSettingDefault = sectionAttributeSettingResultSet.SectionAttributeSettings;
                List<PAMSectionAttributeSetting> lstSectionAttributeSettingFields = new List<PAMSectionAttributeSetting>();

                foreach (PAMSectionAttributeSetting obj in lstSectionAttributeSettingDefault)
                {
                    lstSectionAttributeSettingFields.AddRange(obj.children);
                }

                Guid entitySettingIdGUID = new Guid(entitySettingId);
                Guid sessionIdGUID = new Guid(sessionId);

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                var lstResult = (from ss in pam2EntitiesContext.SessionSections
                                 join s in pam2EntitiesContext.Sections on ss.SectionId equals s.SectionId
                                 join a in pam2EntitiesContext.AttributeSettings on s.SectionId equals a.SectionId
                                    into result
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby s.DisplayOrder, index
                                 where ss.SessionId == sessionIdGUID && s.EntitySettingId == entitySettingIdGUID
                                 select new { Section = s, AttrSetting }
                                    ).ToList();


                PAMSectionAttributeSetting parentNode = new PAMSectionAttributeSetting();
                Guid previousSectiond = Guid.Empty;
                List<PAMSectionAttributeSetting> lstChildren = new List<PAMSectionAttributeSetting>();

                if (lstResult != null && lstResult.Count > 0)
                {
                    foreach (var obj in lstResult)
                    {
                        if (obj == null)
                            continue;
                        PAMSectionAttributeSetting childNode = new PAMSectionAttributeSetting();

                        if (previousSectiond == Guid.Empty || (obj.Section.SectionId != null && obj.Section.SectionId != Guid.Empty && lstSectionAttributeSetting.Find(s => s.SectionId == obj.Section.SectionId.ToString()) == null))
                        {
                            parentNode = new PAMSectionAttributeSetting();
                            parentNode.children = new List<PAMSectionAttributeSetting>();
                            lstChildren = new List<PAMSectionAttributeSetting>();
                            parentNode.leaf = false;
                            parentNode.text = obj.Section.SectionName;
                            parentNode.cls = "folder";
                            parentNode.EntitySettingId = obj.Section.EntitySettingId.ToString();
                            parentNode.GroupName = obj.Section.SectionName;
                            parentNode.id = obj.Section.SectionId.ToString();
                            //parentCount += 1;
                            //parentNode.MatchAttributeSettingId = parentCount.ToString();
                            //  parentNode.MatchGroupId = obj.SectionId.ToString();
                            parentNode.SectionId = obj.Section.SectionId.ToString();
                            parentNode.qtip = obj.Section.SectionName;
                            parentNode.DisplayName = obj.Section.SectionName;
                            parentNode.SchemaName = "";
                            parentNode.DisplayOrder = Convert.ToString(obj.Section.DisplayOrder);
                            parentNode.SessionId = sessionId;

                            PAMSectionAttributeSetting defaultSection = lstSectionAttributeSettingDefault.Where(c => c.DisplayName.ToLower().Trim() == parentNode.DisplayName.ToLower().Trim()).FirstOrDefault<PAMSectionAttributeSetting>();
                            if (defaultSection != null)
                            {
                                parentNode.BestFieldDetectionSettings = defaultSection.BestFieldDetectionSettings;
                                // Use this field to check whether the section is from default settings
                                parentNode.DataType = "true";
                            }

                            lstSectionAttributeSetting.Add(parentNode);
                        }

                        if (obj.AttrSetting != null && obj.AttrSetting.AttributeSettingId != Guid.Empty && obj.AttrSetting.CustomName.ToLower() != "header")
                        {
                            childNode.leaf = true;
                            childNode.text = obj.AttrSetting.DisplayName;
                            childNode.cls = "file";
                            childNode.EntitySettingId = obj.AttrSetting.EntitySettingId.ToString();
                            childNode.GroupName = obj.Section.SectionName;
                            childNode.id = obj.AttrSetting.AttributeSettingId.ToString();
                            childNode.MatchAttributeSettingId = obj.AttrSetting.AttributeSettingId.ToString();
                            //     childNode.MatchGroupId = obj.SectionId.ToString();
                            childNode.SectionId = obj.AttrSetting.SectionId.ToString();
                            childNode.qtip = obj.AttrSetting.DisplayName;
                            childNode.SchemaName = obj.AttrSetting.SchemaName;
                            childNode.DisplayName = obj.AttrSetting.DisplayName;
                            childNode.CustomName = obj.AttrSetting.CustomName;
                            childNode.ExcludeUpdate = obj.AttrSetting.ExcludeUpdate;
                            childNode.DisplayOrder = Convert.ToString(obj.AttrSetting.DisplayOrder);
                            childNode.IsVisible = obj.AttrSetting.IsVisible;
                            childNode.SessionId = sessionId;

                        //    PAMSectionAttributeSetting defaultField = lstSectionAttributeSettingDefault.Where(c => c.DisplayName.ToLower().Trim() == childNode.DisplayName.ToLower().Trim()).FirstOrDefault<PAMSectionAttributeSetting>();
                            PAMSectionAttributeSetting defaultField = lstSectionAttributeSettingFields.Where(c => c.DisplayName.ToLower().Trim() == childNode.DisplayName.ToLower().Trim()).FirstOrDefault<PAMSectionAttributeSetting>();

                            if (defaultField != null)
                            {
                                childNode.BestFieldDetectionSettings = defaultField.BestFieldDetectionSettings;
                                // Use this field to check whether the attribute is from default settings
                                childNode.DataType = "true";
                            }

                            lstChildren.Add(childNode);
                            parentNode.children = lstChildren;
                        }

                        previousSectiond = obj.Section.SectionId;
                    }

                    sectionAttributeSettingResultSet.Message = "Success";
                    sectionAttributeSettingResultSet.SectionAttributeSettings = lstSectionAttributeSetting.Distinct().OrderBy(p => p.DisplayOrder).ToList<PAMSectionAttributeSetting>();
                    sectionAttributeSettingResultSet.Result = true;
                    sectionAttributeSettingResultSet.success = true;
                    sectionAttributeSettingResultSet.total = lstSectionAttributeSetting.Count;

                }
                else
                {
                    //  sectionAttributeSettingResultSet = GetDefaultSessionSectionsAttributeSettings(entitySettingId);
                    sectionAttributeSettingResultSet = GetSectionsAttributeSettingsWithRules(entitySettingId, true);
                    return sectionAttributeSettingResultSet;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionAttributeSettingResultSet;
        }
        private MatchGroupAttributeSettingResultSet GetDefaultSessionSectionsAttributeSettings(string entitySettingId)
        {
            MatchGroupAttributeSettingResultSet sectionAttributeSettingResultSet = new MatchGroupAttributeSettingResultSet();
            List<PAMMatchGroupAttributeSetting> lstSectionAttributeSetting = new List<PAMMatchGroupAttributeSetting>();

            try
            {
                PAMMatchGroupAttributeSetting parentNode = new PAMMatchGroupAttributeSetting();
                Guid previousSectiond = Guid.Empty;
                List<PAMMatchGroupAttributeSetting> lstChildren = new List<PAMMatchGroupAttributeSetting>();
                var entityDisplaySetting = GetSectionsAttributeSettings(entitySettingId);
                if (entityDisplaySetting != null && entityDisplaySetting.MatchGroupAttributeSettings != null && entityDisplaySetting.MatchGroupAttributeSettings.Count > 0)
                {
                    foreach (var obj in entityDisplaySetting.MatchGroupAttributeSettings)
                    {
                        if (obj == null)
                            continue;

                        if (previousSectiond == Guid.Empty || (obj.SectionId != null && !string.IsNullOrWhiteSpace(obj.SectionId) && previousSectiond.ToString() != obj.SectionId))
                        {
                            parentNode = new PAMMatchGroupAttributeSetting();
                            parentNode.children = new List<PAMMatchGroupAttributeSetting>();
                            lstChildren = new List<PAMMatchGroupAttributeSetting>();
                            parentNode.leaf = false;
                            parentNode.text = obj.GroupName;
                            parentNode.cls = "folder";
                            parentNode.EntitySettingId = obj.EntitySettingId.ToString();
                            parentNode.GroupName = obj.GroupName;
                            parentNode.id = obj.SectionId.ToString();
                            parentNode.SectionId = obj.SectionId;
                            parentNode.qtip = obj.GroupName;
                            parentNode.DisplayName = obj.DisplayName;
                            parentNode.SchemaName = "";
                            parentNode.DisplayOrder = Convert.ToString(obj.DisplayOrder);
                            lstSectionAttributeSetting.Add(parentNode);
                        }
                        if (obj.children != null && obj.children.Count > 0)
                        {
                            foreach (var item in obj.children)
                            {
                                PAMMatchGroupAttributeSetting childNode = new PAMMatchGroupAttributeSetting();

                                childNode.leaf = true;
                                childNode.text = item.DisplayName;
                                childNode.cls = "file";
                                childNode.EntitySettingId = item.EntitySettingId;
                                childNode.GroupName = item.GroupName;
                                childNode.id = item.MatchAttributeSettingId;
                                childNode.MatchAttributeSettingId = item.MatchAttributeSettingId;
                                childNode.SectionId = item.SectionId.ToString();
                                childNode.qtip = item.DisplayName;
                                childNode.SchemaName = item.SchemaName;
                                childNode.DisplayName = item.DisplayName;
                                childNode.CustomName = item.CustomName;
                                childNode.ExcludeUpdate = item.ExcludeUpdate;
                                childNode.IsVisible = item.IsVisible;
                                childNode.DisplayOrder = Convert.ToString(item.DisplayOrder);
                                lstChildren.Add(childNode);
                                parentNode.children = lstChildren;
                            }
                        }

                        previousSectiond = new Guid(obj.SectionId);
                    }
                }

                sectionAttributeSettingResultSet.Message = "Success";
                sectionAttributeSettingResultSet.MatchGroupAttributeSettings = lstSectionAttributeSetting.OrderBy(p => p.DisplayOrder).ToList<PAMMatchGroupAttributeSetting>();
                sectionAttributeSettingResultSet.Result = true;
                sectionAttributeSettingResultSet.success = true;
                sectionAttributeSettingResultSet.total = lstSectionAttributeSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sectionAttributeSettingResultSet;
        }

        public AttributeSetting AddSessionDisplaySettings(PAMMatchGroupAttributeSetting attributeDetails, Guid pamUserId, string sessionId)
        {
            AttributeSetting addedAttributeSetting = null;
            AttributeSetting foundAttributeSetting = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (!string.IsNullOrWhiteSpace(sessionId) && attributeDetails.MatchAttributeSettingId != null && !string.IsNullOrWhiteSpace(attributeDetails.MatchAttributeSettingId) && attributeDetails.EntitySettingId != null && !string.IsNullOrWhiteSpace(attributeDetails.EntitySettingId) && !string.IsNullOrEmpty(attributeDetails.SectionId))
                {
                    Guid entitySettingIdGUID = new Guid(attributeDetails.EntitySettingId);
                    Guid sectionIdGuid = new Guid(attributeDetails.SectionId);
                    Guid attributeSettingIdGuid = new Guid(attributeDetails.MatchAttributeSettingId);
                    Guid sessionIdGuid = new Guid(sessionId);
                    foundAttributeSetting = pam2EntitiesContext.AttributeSettings.Where(attributeSetting => attributeSetting.SessionId == sessionIdGuid && attributeSetting.AttributeSettingId == attributeSettingIdGuid && attributeSetting.EntitySettingId == entitySettingIdGUID && attributeSetting.SectionId == sectionIdGuid && attributeSetting.CustomName != "Header").FirstOrDefault();
                }
                if (foundAttributeSetting != null)
                {
                    foundAttributeSetting.DisplayName = attributeDetails.DisplayName;
                    foundAttributeSetting.UpdateDate = DateTime.UtcNow;
                    foundAttributeSetting.CustomName = attributeDetails.CustomName;
                    int displayOrder = 0;
                    if (!string.IsNullOrWhiteSpace(attributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(attributeDetails.DisplayOrder, out displayOrder);
                    }
                    foundAttributeSetting.DisplayOrder = displayOrder;
                    foundAttributeSetting.ExcludeUpdate = attributeDetails.ExcludeUpdate;
                    foundAttributeSetting.IsVisible = attributeDetails.IsVisible;
                    foundAttributeSetting.SectionId = new Guid(attributeDetails.SectionId);
                    // this needs to be implemented when integrated with CRM
                    // foundAttributeSetting.UpdatedBy = pamUserId;
                    foundAttributeSetting.EntitySettingId = new Guid(attributeDetails.EntitySettingId);
                    foundAttributeSetting.SchemaName = attributeDetails.SchemaName;
                    foundAttributeSetting.SessionId = new Guid(sessionId);
                    pam2EntitiesContext.SaveChanges();
                    addedAttributeSetting = foundAttributeSetting;
                }
                else
                {
                    AttributeSetting newAttributeSetting = new AttributeSetting();
                    Guid sessionIdGuid = new Guid(sessionId);
                    newAttributeSetting.DisplayName = attributeDetails.DisplayName;
                    newAttributeSetting.CustomName = attributeDetails.CustomName;
                    int displayOrder = 0;
                    if (!string.IsNullOrWhiteSpace(attributeDetails.DisplayOrder))
                    {
                        Int32.TryParse(attributeDetails.DisplayOrder, out displayOrder);
                    }
                    newAttributeSetting.DisplayOrder = displayOrder;
                    newAttributeSetting.ExcludeUpdate = attributeDetails.ExcludeUpdate;
                    newAttributeSetting.IsVisible = attributeDetails.IsVisible;
                    newAttributeSetting.SectionId = new Guid(attributeDetails.SectionId);
                    newAttributeSetting.EntitySettingId = new Guid(attributeDetails.EntitySettingId);
                    newAttributeSetting.SchemaName = attributeDetails.SchemaName;
                    newAttributeSetting.SessionId = sessionIdGuid;
                    //this field need to be taken from extjs and will be sent across request and applied
                    //newMatchGroupAttributeSetting.CreatedBy =  pamUserId;
                    newAttributeSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.AttributeSettings.Add(newAttributeSetting);
                    pam2EntitiesContext.SaveChanges();
                    addedAttributeSetting = newAttributeSetting;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedAttributeSetting;
        }

        public ResultSet DeleteSessionDisplayAttributeSetting(string AttributeSettingId, string sessionId)
        {
            var attributeSettingResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                AttributeSetting attributeSetting = null;
                Guid attributeSettingIdGuid = new Guid(AttributeSettingId);
                Guid sessionIdGuid = new Guid(sessionId);
                attributeSetting = pam2EntitiesContext.AttributeSettings.Where(s => s.AttributeSettingId == attributeSettingIdGuid && s.SessionId == sessionIdGuid).FirstOrDefault();
                if (attributeSetting != null)
                {
                    attributeSetting = pam2EntitiesContext.AttributeSettings.Remove(attributeSetting);
                    int count = pam2EntitiesContext.SaveChanges();
                    attributeSettingResult.Message = "Success";
                    attributeSettingResult.Result = true;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return attributeSettingResult;
        }

        public ResultSet DeleteAllSessionDisplayAttributeSetting(Guid SectionId)
        {
            var attributeSettingResult = new ResultSet();
            try
            {
                var pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<AttributeSetting> attributeSettingList = null;
                attributeSettingList = pam2EntitiesContext.AttributeSettings.Where(s => s.SectionId == SectionId).ToList();
                if (attributeSettingList != null && attributeSettingList.Count > 0)
                {
                    foreach (var attribute in attributeSettingList)
                    {
                        pam2EntitiesContext.AttributeSettings.Remove(attribute);
                        int count = pam2EntitiesContext.SaveChanges();
                    }

                }
                attributeSettingResult.Message = "Success";
                attributeSettingResult.Result = true;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return attributeSettingResult;
        }


        #endregion

        #region Priority

        public PriorityResultSet GetPriorities()
        {

            PriorityResultSet result = new PriorityResultSet();
            List<PAMPriority> priorityList = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                priorityList = (from priority in pam2EntitiesContext.PriorityMasters
                                orderby priority.Priority
                                select priority).AsEnumerable().Select(pampriority => new PAMPriority
                                {
                                    Name = pampriority.Name,
                                    Description = pampriority.Description,
                                    Id = pampriority.PriorityId
                                }
                                ).ToList();

                result.Message = "Success";
                result.Result = true;
                result.Priorities = priorityList;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return result;
            //      return priorityList;
        }

        #endregion

        #region PAM Language Master

        public PAMLanguageResultSet GetLanguages()
        {
            PAMLanguageResultSet result = new PAMLanguageResultSet();
            List<PAMLanguage> languageList = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                languageList = pam2EntitiesContext.LanguageMasters.AsEnumerable().Select<LanguageMaster, PAMLanguage>(new Func<LanguageMaster, PAMLanguage>(language => new PAMLanguage
                {
                    LanguageName = language.LanguageName,
                    LanguageId = language.LanguageId
                })).OrderBy(x => x.LanguageName).ToList();

                result.Result = true;
                result.PAMLanguages = languageList;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return result;
        }

        public LanguageMaster PAMLanguageResultGetByID(string LangID)
        {
            Guid SessionGuid = new Guid(LangID);
            PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
            LanguageMaster obj = pam2EntitiesContext.LanguageMasters.Where(p => p.LanguageId == SessionGuid).FirstOrDefault();

            return obj;
        }

        public PAMMatchKeyResultSet GetMatchKeys()
        {
            PAMMatchKeyResultSet result = new PAMMatchKeyResultSet();
            List<PAMMatchKeys> lstMatchKeyMaster = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstMatchKeyMaster = pam2EntitiesContext.MatchKeyMasters.AsEnumerable().Select<MatchKeyMaster, PAMMatchKeys>(new Func<MatchKeyMaster, PAMMatchKeys>(matchkey => new PAMMatchKeys
                {
                    MatchKey = matchkey.MatchKey,
                    MatchKeyID = matchkey.MatchKeyID
                })).OrderBy(x => x.MatchKey).ToList();

                result.Message = "Success";
                result.Result = true;
                result.PAMMatchKeys = lstMatchKeyMaster;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return result;
        }

        #endregion


        #region Theme Master

        public PAMThemeResultSet GetThemes()
        {
            PAMThemeResultSet result = new PAMThemeResultSet();
            List<PAMTheme> themeList = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                themeList = pam2EntitiesContext.ThemeMasters.AsEnumerable().Select<ThemeMaster, PAMTheme>(new Func<ThemeMaster, PAMTheme>(theme => new PAMTheme
                {
                    Name = theme.Name,
                    ThemeId = theme.ThemeId,
                    ThemeFileName = theme.ThemeFileName,
                    IsApplied = theme.IsApplied
                })).OrderBy(x => x.Name).ToList();

                result.Result = true;
                result.PAMThemes = themeList;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return result;
        }

        public ResultSet UpdateTheme(string ThemeId)
        {
            ResultSet themeResult = new ResultSet();
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                ThemeMaster objThemeMaster = objPAM2EntitiesContext.ThemeMasters.Where(p => p.IsApplied == true).SingleOrDefault();
                objThemeMaster.IsApplied = false;
                objPAM2EntitiesContext.SaveChanges();

                Guid gThemeId = new Guid(ThemeId);
                ThemeMaster objThemeMasterNew = objPAM2EntitiesContext.ThemeMasters.Where(p => p.ThemeId == gThemeId).SingleOrDefault();
                objThemeMasterNew.IsApplied = true;
                objPAM2EntitiesContext.SaveChanges();

                themeResult.Result = true;
                themeResult.success = true;
                themeResult.Message = "Success";
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return themeResult;
        }

        #endregion


        #region Session Threshold Setting

        public PAMSessionThresholdSettingResultSet GetSessionThresholdSettings(string sessionId)
        {
            PAMSessionThresholdSettingResultSet sessionThresholdSettingResultSet = new PAMSessionThresholdSettingResultSet();
            List<PAMSessionThresholdSetting> lstSessionThresholdSetting = new List<PAMSessionThresholdSetting>();
            try
            {
                Guid sessionIdGuid = new Guid(sessionId);
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                lstSessionThresholdSetting = pam2EntitiesContext.SessionThresholdSettings.Where<SessionThresholdSetting>(new Func<SessionThresholdSetting, bool>(setting => setting.SessionId == sessionIdGuid)).AsEnumerable().Select<SessionThresholdSetting, PAMSessionThresholdSetting>(new Func<SessionThresholdSetting, PAMSessionThresholdSetting>(thresholdSetting => new PAMSessionThresholdSetting
                    {
                        IncludeOriginal = thresholdSetting.IncludeOriginal,
                        LanguageId = thresholdSetting.LanguageId,
                        SessionId = thresholdSetting.SessionId.ToString(),
                        SessionThresholdId = thresholdSetting.SessionThresholdId.ToString(),
                        MatchKeyID = thresholdSetting.MatchKeyID,
                        Threshold1 = thresholdSetting.Threshold1,
                        Threshold2 = thresholdSetting.Threshold2,
                        TreatAccountNulls = thresholdSetting.TreatAccountNulls,
                        TreatNulls = thresholdSetting.TreatNulls,
                        ViewType = thresholdSetting.ViewType,
                        ViewName = thresholdSetting.ViewName,
                        FetchXML = thresholdSetting.FetchXML,
                        ValueToNull = thresholdSetting.ValueToNull,
                        MatchKey = "" // thresholdSetting.MatchKeyMaster.MatchKey
                    })).ToList();
                sessionThresholdSettingResultSet.Message = "Success";
                sessionThresholdSettingResultSet.PAMSessionThresholdSettings = lstSessionThresholdSetting;
                sessionThresholdSettingResultSet.Result = true;
                sessionThresholdSettingResultSet.total = lstSessionThresholdSetting.Count;
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return sessionThresholdSettingResultSet;
        }

        public SessionThresholdSetting AddSessionThresholdSettingNew(PAMSessionThresholdSetting sessionThresholdSetting, Guid pamUserId, string sessionId)
        {
            SessionThresholdSetting addedSessionThresholdSetting = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                SessionThresholdSetting newSessionThresholdSetting = new SessionThresholdSetting();

                newSessionThresholdSetting.IncludeOriginal = sessionThresholdSetting.IncludeOriginal;
                newSessionThresholdSetting.LanguageId = sessionThresholdSetting.LanguageId;
                newSessionThresholdSetting.SessionId = new Guid(sessionId); // !string.IsNullOrWhiteSpace(sessionThresholdSetting.SessionId) ? new Guid(sessionThresholdSetting.SessionId.ToString()) : Guid.Empty;
                newSessionThresholdSetting.Threshold1 = sessionThresholdSetting.Threshold1;
                newSessionThresholdSetting.Threshold2 = sessionThresholdSetting.Threshold2;
                //  newSessionThresholdSetting.MatchKeyID = sessionThresholdSetting.MatchKeyID;
                /***************************************
                 * Modified by: Sameer Ahire
                 * Modified date: 13-May-2016
                 * Reason: Added new checkbox for "Match contacts at the same company only".
                 *         Account_ID of records of same company will only be considered for merging
                 ************************************/         
                newSessionThresholdSetting.TreatAccountNulls = sessionThresholdSetting.TreatAccountNulls;
                /****************** end *************************/
                newSessionThresholdSetting.TreatNulls = sessionThresholdSetting.TreatNulls;
                newSessionThresholdSetting.ValueToNull = sessionThresholdSetting.ValueToNull;
                //this field need to be taken from extjs and will be sent across request and applied
                newSessionThresholdSetting.ViewType = sessionThresholdSetting.ViewType;
                newSessionThresholdSetting.ViewName = sessionThresholdSetting.ViewName;
                newSessionThresholdSetting.FetchXML = sessionThresholdSetting.FetchXML;

                newSessionThresholdSetting.CreatedBy = pamUserId;
                newSessionThresholdSetting.CreatedDate = DateTime.UtcNow;
                pam2EntitiesContext.SessionThresholdSettings.Add(newSessionThresholdSetting);
                pam2EntitiesContext.SaveChanges();
                addedSessionThresholdSetting = newSessionThresholdSetting;
            }
            catch (Exception)
            {
                throw;
            }
            return addedSessionThresholdSetting;
        }

        public SessionThresholdSetting AddSessionThresholdSetting(PAMSessionThresholdSetting sessionThresholdSetting, Guid pamUserId, string sessionId)
        {
            SessionThresholdSetting addedSessionThresholdSetting = null;
            SessionThresholdSetting foundSessionThresholdSetting = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (sessionThresholdSetting.SessionThresholdId != null && !string.IsNullOrWhiteSpace(sessionThresholdSetting.SessionThresholdId) && sessionThresholdSetting.SessionId != null && !string.IsNullOrWhiteSpace(sessionThresholdSetting.SessionId))
                {
                    foundSessionThresholdSetting = pam2EntitiesContext.SessionThresholdSettings.Where(thresholdSetting => thresholdSetting.SessionThresholdId == new Guid(sessionThresholdSetting.SessionThresholdId) && thresholdSetting.SessionId == new Guid(sessionThresholdSetting.SessionId)).FirstOrDefault();
                }
                if (foundSessionThresholdSetting != null)
                {
                    foundSessionThresholdSetting.IncludeOriginal = sessionThresholdSetting.IncludeOriginal;
                    foundSessionThresholdSetting.LanguageId = sessionThresholdSetting.LanguageId;
                    foundSessionThresholdSetting.SessionId = new Guid(sessionId);// !string.IsNullOrWhiteSpace(sessionThresholdSetting.SessionId) ? new Guid(sessionThresholdSetting.SessionId.ToString()) : Guid.Empty;
                    foundSessionThresholdSetting.Threshold1 = sessionThresholdSetting.Threshold1;
                    foundSessionThresholdSetting.Threshold2 = sessionThresholdSetting.Threshold2;
                    //  foundSessionThresholdSetting.MatchKeyID = sessionThresholdSetting.MatchKeyID;
                    foundSessionThresholdSetting.TreatAccountNulls = sessionThresholdSetting.TreatAccountNulls;
                    foundSessionThresholdSetting.TreatNulls = sessionThresholdSetting.TreatNulls;
                    foundSessionThresholdSetting.ValueToNull = sessionThresholdSetting.ValueToNull;
                    foundSessionThresholdSetting.ViewType = sessionThresholdSetting.ViewType;
                    foundSessionThresholdSetting.ViewName = sessionThresholdSetting.ViewName;
                    foundSessionThresholdSetting.FetchXML = sessionThresholdSetting.FetchXML;

                    foundSessionThresholdSetting.UpdatedBy = pamUserId;
                    foundSessionThresholdSetting.UpdatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SaveChanges();
                    addedSessionThresholdSetting = foundSessionThresholdSetting;
                }
                else
                {
                    SessionThresholdSetting newSessionThresholdSetting = new SessionThresholdSetting();

                    newSessionThresholdSetting.IncludeOriginal = sessionThresholdSetting.IncludeOriginal;
                    newSessionThresholdSetting.LanguageId = sessionThresholdSetting.LanguageId;
                    newSessionThresholdSetting.SessionId = new Guid(sessionId); // !string.IsNullOrWhiteSpace(sessionThresholdSetting.SessionId) ? new Guid(sessionThresholdSetting.SessionId.ToString()) : Guid.Empty;
                    newSessionThresholdSetting.Threshold1 = sessionThresholdSetting.Threshold1;
                    newSessionThresholdSetting.Threshold2 = sessionThresholdSetting.Threshold2;
                    //  newSessionThresholdSetting.MatchKeyID = sessionThresholdSetting.MatchKeyID;
                    newSessionThresholdSetting.TreatAccountNulls = sessionThresholdSetting.TreatAccountNulls;
                    newSessionThresholdSetting.TreatNulls = sessionThresholdSetting.TreatNulls;
                    newSessionThresholdSetting.ValueToNull = sessionThresholdSetting.ValueToNull;
                    //this field need to be taken from extjs and will be sent across request and applied

                    newSessionThresholdSetting.ViewType = sessionThresholdSetting.ViewType;
                    newSessionThresholdSetting.ViewName = sessionThresholdSetting.ViewName;
                    newSessionThresholdSetting.FetchXML = sessionThresholdSetting.FetchXML;

                    newSessionThresholdSetting.CreatedBy = pamUserId;
                    newSessionThresholdSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SessionThresholdSettings.Add(newSessionThresholdSetting);
                    pam2EntitiesContext.SaveChanges();
                    addedSessionThresholdSetting = newSessionThresholdSetting;
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return addedSessionThresholdSetting;
        }



        #endregion

        #region RuleEngine And Match Engine

        public IEnumerable<GroupRule> getGroupRulesForMatch(string sessionID)
        {
            List<GroupRule> lstGroupRule = new List<GroupRule>();
            SqlConnection con = new SqlConnection(sqlConnString);
            try
            {

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                SqlCommand cmd = new SqlCommand(@"select GR.GroupRuleId,GR.GroupId,GR.RuleId,GR.IsMaster,GR.[Order],GR.[Description],mrm.enum RuleEnumName
                        from GroupRule GR inner join SessionGroup sg on sg.MatchGroupId = gr.GroupId
                        inner join matchRulemaster mrm on mrm.MatchRuleId = GR.RuleId
                        where sg.SessionId = @SessionId", con);
                cmd.Parameters.AddWithValue("@SessionId", sessionID);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lstGroupRule.Add(new GroupRule()
                    {
                        GroupRuleId = new Guid(Convert.ToString(dr["GroupRuleId"])),
                        GroupId = new Guid(Convert.ToString(dr["GroupId"])),
                        IsMaster = dr.GetBoolean(3),
                        Order = dr.GetInt32(4),
                        Description = Convert.ToString(dr["Description"]),
                        RuleEnumName = Convert.ToString(dr["RuleEnumName"]),
                        GroupRuleDetails = this.getGroupRulesDetailForMatch(Convert.ToString(dr["GroupRuleId"]))
                    });
                }
                con.Close();

            }
            catch (Exception ex)
            {
                con.Close();
                throw ex;

            }

            return lstGroupRule;
        }


        private ICollection<GroupRuleDetail> getGroupRulesDetailForMatch(string GroupRuleid)
        {
            List<GroupRuleDetail> lstGroupRule = new List<GroupRuleDetail>();
            SqlConnection con = new SqlConnection(sqlConnString);
            try
            {

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                SqlCommand cmd = new SqlCommand(@"select * from GroupRuleDetail where GroupRuleId = @GroupRuleid", con);
                cmd.Parameters.AddWithValue("@GroupRuleid", GroupRuleid);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lstGroupRule.Add(new GroupRuleDetail()
                    {
                        GroupRuleDetailId = new Guid(Convert.ToString(dr["GroupRuleDetailId"])),
                        GroupRuleId = new Guid(Convert.ToString(dr["GroupRuleId"])),
                        AttributeEnum = Convert.ToString(dr["AttributeEnum"]),
                        AttributeValue = Convert.ToString(dr["AttributeValue"])
                    });
                }

                con.Close();
            }
            catch (Exception ex)
            {
                con.Close();
                throw ex;
            }

            return lstGroupRule;
        }

        public IEnumerable<MatchAttributeSetting> getMatchAttributeSettingsForMatch(string sessionID)
        {
            List<MatchAttributeSetting> IemMatchAttributeSetting = new List<MatchAttributeSetting>();
            SqlConnection con = new SqlConnection(sqlConnString);
            try
            {
                //PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                //IemMatchAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.AsEnumerable().Where(p => p.SessionId == Guid.Parse(sessionID)).Distinct();

                //                SqlCommand cmd = new SqlCommand(@"select distinct mas.MatchAttributeSettingId,mas.EntitySettingId,mas.MatchGroupId,mas.SchemaName,mas.DisplayName,mas.SessionId,
                //                                            mas.CreatedBy,mas.CreatedDate,mas.UpdatedBy,mas.UpdateDate,mg.DisplayName GroupDisplayName,pm.Name PriorityName,
                //                                            pm.Priority Priority,
                //                                            sg.ExcludeFromMasterKey, MKM.MatchKey
                //                                            from MatchAttributeSetting 
                //                                            mas inner join matchgroup mg on mg.MatchGroupId = mas.MatchGroupId
                //                                            inner join SessionGroup sg on sg.SessionId = mas.sessionid
                //                                            inner join MatchKeyMaster MKM on sg.MatchKeyID= MKM.MatchKeyID
                //                                            inner join PriorityMaster pm on pm.PriorityId = sg.PriorityId where mas.SessionId = @SessionId", con);

                SqlCommand cmd = new SqlCommand(@"select distinct mas.MatchAttributeSettingId,mas.EntitySettingId,mas.MatchGroupId,mas.SchemaName,mas.DisplayName,mas.SessionId,
                                        mas.CreatedBy,mas.CreatedDate,mas.UpdatedBy,mas.UpdateDate,
										temp.GroupDisplayName,temp.PriorityName,
                                        temp.[Priority] Priority,
                                        temp.ExcludeFromMasterKey, temp.MatchKey
                                        from MatchAttributeSetting mas inner join
                            (
            select mg.DisplayName GroupDisplayName, sg.MatchGroupId, sg.ExcludeFromMasterKey,sg.MatchKeyID, MKM.Enum MatchKey
        ,pm.Name PriorityName, pm.[Priority] Priority
        from matchgroup mg
        inner join SessionGroup sg on mg.MatchGroupId = sg.MatchGroupId
        inner join MatchKeyMaster MKM on sg.MatchKeyID= MKM.MatchKeyID
        inner join PriorityMaster pm on pm.PriorityId = sg.PriorityId where sg.SessionId = @SessionId
							)temp on mas.MatchGroupId = temp.MatchGroupId
        where mas.SessionId =@SessionId
            order by SchemaName", con);


                cmd.Parameters.AddWithValue("@SessionId", sessionID);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    IemMatchAttributeSetting.Add(new MatchAttributeSetting()
                    {
                        MatchAttributeSettingId = new Guid(Convert.ToString(dr["MatchAttributeSettingId"])),
                        EntitySettingId = new Guid(Convert.ToString(dr["EntitySettingId"])),
                        MatchGroupId = new Guid(Convert.ToString(dr["MatchGroupId"])),
                        SchemaName = Convert.ToString(dr["SchemaName"]),
                        DisplayName = Convert.ToString(dr["DisplayName"]),
                        SessionId = new Guid(Convert.ToString(dr["SessionId"])),
                        GroupDisplayName = Convert.ToString(dr["GroupDisplayName"]),
                        PriorityName = Convert.ToString(dr["PriorityName"]),
                        Priority = String.IsNullOrEmpty(Convert.ToString(dr["Priority"])) ? "0" : Convert.ToString(dr["Priority"]),
                        ExcludeFromMasterKey = Convert.ToBoolean(dr["ExcludeFromMasterKey"]),
                        MatchKey = Convert.ToString(dr["MatchKey"])
                    });
                }
                con.Close();
            }
            catch (Exception ex)
            {
                con.Close();
                throw ex;

            }

            return IemMatchAttributeSetting;
        }

        public IEnumerable<GroupRule> getGroupRules(string sessionID)
        {
            IEnumerable<GroupRule> lstGroupRule = null;
            try
            {

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                if (!string.IsNullOrWhiteSpace(sessionID))
                {
                    lstGroupRule = pam2EntitiesContext.GroupRules.Join(pam2EntitiesContext.SessionGroups, GR => GR.GroupId, SG => SG.MatchGroupId, (GR, SG) =>
                        new
                        {
                            GroupRuleId = GR.GroupRuleId,
                            MatchGroupId = SG.MatchGroupId,
                            MatchRuleId = GR.RuleId,
                            IsMaster = GR.IsMaster,
                            SessionID = SG.SessionId,
                            GroupRuleDetails = GR.GroupRuleDetails,
                            MatchRuleMaster = GR.MatchRuleMaster
                        }).AsEnumerable().Where(p => p.SessionID == Guid.Parse(sessionID)).Distinct().Select(GR => new GroupRule()
                        {
                            GroupRuleId = GR.GroupRuleId,
                            IsMaster = GR.IsMaster,
                            RuleId = GR.MatchRuleId,
                            GroupId = new Guid(GR.MatchGroupId.ToString()),
                            GroupRuleDetails = GR.GroupRuleDetails,
                            MatchRuleMaster = GR.MatchRuleMaster
                        }).AsEnumerable<GroupRule>();


                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstGroupRule;
        }

        public IEnumerable<MatchAttributeSetting> getMatchAttributeSettings(string sessionID)
        {
            IEnumerable<MatchAttributeSetting> IemMatchAttributeSetting = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                IemMatchAttributeSetting = pam2EntitiesContext.MatchAttributeSettings.AsEnumerable().Where(p => p.SessionId == Guid.Parse(sessionID)).Distinct();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return IemMatchAttributeSetting;
        }

        public string UpdatePathForSession(string SessionID, string Path = "", string MergeStatus = "", int GroupCount = 0, string ExecutedBy = "", DateTime? ExecutedOn = null, DateTime? ExecutionEnd = null)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Session objSession = pam2EntitiesContext.Sessions.Where(p => p.SessionId == new Guid(SessionID)).SingleOrDefault();
                if (objSession != null)
                {
                    if (Path != "")
                        objSession.OutputFilePath = Path;
                    if (MergeStatus != "")
                        objSession.MergeStatus = pam2EntitiesContext.Status.Where(p => p.Name.Equals(MergeStatus)).SingleOrDefault().StatusId;
                    if (GroupCount > 0)
                        objSession.GroupCount = GroupCount;
                    if (ExecutedBy != "")
                        objSession.ExecutedBy = new Guid(ExecutedBy);
                    if (ExecutedOn != null)
                        objSession.ExecutedOn = ExecutedOn;
                    if (ExecutionEnd != null)
                        objSession.ExecutionEnd = ExecutionEnd;
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Path;

        }

        public Session getSession(string sessionID)
        {
            Session objSession = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                objSession = pam2EntitiesContext.Sessions.Where(p => p.SessionId == new Guid(sessionID)).SingleOrDefault();

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objSession;
        }

        public void SaveSessionResult(List<SessionResult> lstSessionResult)
        {
            SqlConnection con = new SqlConnection(sqlConnString);
            try
            {

                SqlDataAdapter da = new SqlDataAdapter("select top 0 * from SessionResult", con);
                DataSet ds = new DataSet();
                object objLock = new object();
                da.Fill(ds);
                foreach (SessionResult obj in lstSessionResult)
                {
                    DataRow row = ds.Tables[0].NewRow();
                    row["SessionResultId"] = obj.SessionResultId;
                    row["SessionId"] = obj.SessionId;
                    row["RecordId"] = obj.RecordId;
                    row["GroupNo"] = obj.GroupNo;
                    if (obj.GroupRank != null)
                        row["GroupRank"] = obj.GroupRank;
                    if (obj.MatchScore != null)
                        row["MatchScore"] = obj.MatchScore;
                    if (obj.PunchIn != null)
                        row["PunchIn"] = obj.PunchIn;
                    if (obj.PunchOut != null)
                        row["PunchOut"] = obj.PunchOut;
                    if (obj.ReviewStatus != null)
                        row["ReviewStatus"] = obj.ReviewStatus;
                    if (obj.Reviewer != null)
                        row["Reviewer"] = obj.Reviewer;
                    if (obj.CurentStatusDateTime != null)
                        row["CurentStatusDateTime"] = obj.CurentStatusDateTime;
                    if (obj.EntityHeaderFieldValue != null)
                        row["EntityHeaderFieldValue"] = obj.EntityHeaderFieldValue;
                    if (obj.IsPrimary != null)
                        row["IsPrimary"] = obj.IsPrimary;
                    if (obj.Status != null)
                        row["Status"] = obj.Status;
                    if (obj.CreatedOn != null)
                        row["CreatedOn"] = obj.CreatedOn;
                    ds.Tables[0].Rows.Add(row);
                }
                SqlCommandBuilder cb = new SqlCommandBuilder(da);
                con.Open();
                da.Update(ds.Tables[0]);
                con.Close();
            }
            catch (Exception ex)
            {
                con.Close();
                throw ex;

            }
        }

        public void SaveSessionResult(SessionResult objSession)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                var obj = pam2EntitiesContext.SessionResults.Where(p => p.SessionResultId == objSession.SessionResultId).SingleOrDefault();
                if (obj != null)
                {
                    //obj.CurentStatusDateTime = objSession.CurentStatusDateTime;
                    //obj.EntityHeaderFieldValue = objSession.EntityHeaderFieldValue;
                    //obj.GroupNo = objSession.GroupNo;
                    obj.GroupRank = objSession.GroupRank;
                    //obj.IsPrimary = objSession.IsPrimary;
                    //obj.MatchScore = obj.MatchScore;
                    //obj.PunchIn = obj.PunchIn;
                    //obj.PunchOut = obj.PunchOut;
                    //obj.RecordId = obj.RecordId;
                    //obj.Reviewer = obj.Reviewer;
                    //obj.ReviewStatus = obj.ReviewStatus;
                    //obj.SessionId = obj.SessionId;
                    //obj.Status = obj.Status;
                }
                else
                {
                    pam2EntitiesContext.SessionResults.Add(objSession);
                }
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public SessionThresholdSetting getSessionThresholdSetting(string SessionID)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                return pam2EntitiesContext.SessionThresholdSettings.AsEnumerable().Where(p => p.SessionId == Guid.Parse(SessionID)).SingleOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Save Session (Combined)

        public ResultSet SaveSession(string sessionID)
        {
            ResultSet objResultSet = null;
            try
            {
                objResultSet = new ResultSet();
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");



            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objResultSet;
        }

        #endregion

        #region Session Results
        public PAMSessionResultSet GetSessionResult(string SessionID, int offset, int count)
        {

            List<PAMSessionResult> lstSessionResult = new List<PAMSessionResult>();
            try
            {
                int a = 1, b = 1;
                decimal g = -1;
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                lstSessionResult = (from u in pam2EntitiesContext.SessionResults
                                    where u.SessionId == new Guid(SessionID)
                                    orderby u.GroupNo
                                    select u).AsEnumerable().Select(ux =>
                             new PAMSessionResult
                             {
                                 SNo = a++,
                                 CreatedOn = (ux.CreatedOn != null ? string.Format("{0:dd-MM-yyyy HH:mm:ss}", ux.CreatedOn) : string.Empty),
                                 GroupRank = ux.GroupRank != null ? ux.GroupRank : 0,
                                 MasterRank = ux.MatchScore != null ? ux.MatchScore : 0,
                                 Primary = Convert.ToBoolean((ux.IsPrimary != null) ? ux.IsPrimary : false),
                                 Name = string.Format("{0:000000.00-}{1:00}", ux.GroupNo, (ux.GroupNo == g ? ++b : b = 1)),
                                 GroupNo = g = ux.GroupNo,
                                 RecordName = string.Empty,//string.Format("{0:000000-}{1:00}", ux.GroupNo, (ux.GroupNo == g?b:b=1)),
                                 ReviewStatus = (ux.Status1 != null ? ux.Status1.Name : ""),
                                 ValidGroup = Convert.ToString(ux.ValidGroup),
                                 RecordID = Convert.ToString(ux.RecordId),
                                 Session = ux.Session.SessionName,
                                 PunchIn = (ux.PunchIn != null ? Convert.ToDateTime(ux.PunchIn).ToString() : ""), // (ux.PunchIn != null ? Convert.ToDateTime(ux.PunchIn).ToString("dd-MMM-yyyy HH:mm") : ""),
                                 PunchOut = (ux.PunchOut != null ? Convert.ToDateTime(ux.PunchOut).ToString() : ""), //(ux.PunchOut != null ? Convert.ToDateTime(ux.PunchOut).ToString("dd-MMM-yyyy HH:mm") : ""),
                                 Reviewer = (ux.User != null ? ux.User.FirstName + " " + ux.User.LastName : ""),
                                 SessionResultId = Convert.ToString(ux.SessionResultId),
                                 CurrentStatusDateTime = (ux.CurentStatusDateTime != null ? string.Format("{0:dd-MM-yyyy HH:mm:ss}", ux.CurentStatusDateTime) : string.Empty),
                                 UserId = (ux.User != null ? Convert.ToString(ux.User.UserId) : ""),
                                 AutoPromote = Convert.ToBoolean(ux.AutoPromote),
                                 AutoFill = Convert.ToBoolean(ux.AutoFill)
                             }).ToList<PAMSessionResult>();



                return new PAMSessionResultSet()
                {
                    Message = "Success",
                    Result = true,
                    SessionResults = lstSessionResult.Skip(offset).Take(count).ToList<PAMSessionResult>(),
                    total = lstSessionResult.Count
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        public void CreateSessionMatchDetail(SessionMatchDetail objSessionMatchDetail)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                SessionMatchDetail _objSessionMatchDetail = pam2EntitiesContext.SessionMatchDetails.Where(p => p.SessionId == objSessionMatchDetail.SessionId).SingleOrDefault();
                if (_objSessionMatchDetail != null)
                {
                    _objSessionMatchDetail.RecordsFed = (objSessionMatchDetail.RecordsFed > 0 ? objSessionMatchDetail.RecordsFed : _objSessionMatchDetail.RecordsFed);
                    _objSessionMatchDetail.DuplicatesFound = (objSessionMatchDetail.DuplicatesFound > 0 ? objSessionMatchDetail.DuplicatesFound : _objSessionMatchDetail.DuplicatesFound);
                    _objSessionMatchDetail.ConfirmedResultCount = (objSessionMatchDetail.ConfirmedResultCount > 0 ? objSessionMatchDetail.ConfirmedResultCount : _objSessionMatchDetail.ConfirmedResultCount);
                    _objSessionMatchDetail.UnsureResultCount = (objSessionMatchDetail.UnsureResultCount > 0 ? objSessionMatchDetail.UnsureResultCount : _objSessionMatchDetail.UnsureResultCount);
                    _objSessionMatchDetail.MatchStatus = Convert.ToString(objSessionMatchDetail.MatchStatus);
                    
                    pam2EntitiesContext.SaveChanges();
                    return;
                }
                else
                {
                    pam2EntitiesContext.SessionMatchDetails.Add(objSessionMatchDetail);
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        public int GetSuppressionCount(string entityLogicalName, string suppressKey)
        {
            int iCount = 0;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                iCount = pam2EntitiesContext.Database.SqlQuery<SuppressionHstory>(@"select * from SuppressionHstory sh
                            inner join EntitySetting es on es.EntitySettingId = sh.EntitySettingId where sh.SuppressKey={0} and es.EntityLogicalName={1}", suppressKey, entityLogicalName).Count();

                //iCount = (from c in pam2EntitiesContext.SuppressionHstories
                //          where c.SuppressKey == suppressKey && c.EntitySetting.EntityLogicalName == entityLogicalName
                //          select c).Count();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return iCount;
        }

        public Status GetStatusRecordByEnum(string Enum)
        {
            Status objStatus = null;

            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objStatus = objPAM2EntitiesContext.Status.Where(status => status.Enum == Enum).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objStatus;
        }

        #region PAM 1

        public PAMSessionResult GetSessionResultRecord(string sessionResutId)
        {


            PAMSessionResult sessionResultRecord = null;
            try
            {
                int a = 1, b = 1;
                decimal g = 0;
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                sessionResultRecord = (from u in pam2EntitiesContext.SessionResults
                                       where u.SessionResultId == new Guid(sessionResutId)
                                       orderby u.GroupNo
                                       select u).AsEnumerable().Select(ux =>
                             new PAMSessionResult
                             {
                                 SNo = a++,
                                 CreatedOn = DateTime.UtcNow.ToString("dd-MMM-yyyy"),
                                 GroupNo = g = ux.GroupNo,
                                 GroupRank = ux.GroupRank != null ? ux.GroupRank : 1,
                                 MasterRank = ux.MatchScore != null ? ux.MatchScore : 0,
                                 Primary = Convert.ToBoolean((ux.IsPrimary != null ? ux.IsPrimary : false)),
                                 Name = string.Format("{0:000000-}{1:00}", ux.GroupNo, (ux.GroupNo == g ? b++ : b = 1)),
                                 RecordName = string.Empty,//string.Format("{0:000000-}{1:00}", ux.GroupNo, (ux.GroupNo == g?b:b=1)),
                                 ReviewStatus = (ux.Status1 != null ? ux.Status1.Description : "NA"),
                                 ValidGroup = "",
                                 RecordID = Convert.ToString(ux.RecordId),
                                 Session = ux.Session.SessionName,
                                 PunchIn = (ux.PunchIn != null ? Convert.ToDateTime(ux.PunchIn).ToString("dd-MMM-yyyy HH:mm") : "NA"),
                                 PunchOut = (ux.PunchOut != null ? Convert.ToDateTime(ux.PunchOut).ToString("dd-MMM-yyyy HH:mm") : "NA"),
                                 Reviewer = (ux.User != null ? ux.User.UserName : "NA")
                             }).FirstOrDefault<PAMSessionResult>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return sessionResultRecord;
        }

        public SessionResult UpdateSessionResult(PAMSessionResult sessionResult, string sessionResultId, string pamUserId)
        {
            SessionResult objSessionResult = null;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSessionResult = objPAM2EntitiesContext.SessionResults.Where(p => p.SessionResultId == new Guid(sessionResultId)).SingleOrDefault();

                if (objSessionResult != null)
                {
                    objSessionResult.Reviewer = new Guid(pamUserId);
                    objSessionResult.PunchIn = DateTime.UtcNow;
                    var reviewStatus = objPAM2EntitiesContext.Status.Where(status => status.Enum.Equals("800")).FirstOrDefault();
                    if (reviewStatus != null)
                    {
                        objSessionResult.ReviewStatus = reviewStatus.StatusId;
                    }
                    objSessionResult.CurentStatusDateTime = DateTime.UtcNow;
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objSessionResult;
        }

        public List<decimal> GetAlltheUnassignedGroups(string SessionId, Guid unProcessedStatusGuiID)
        {
            try
            {
                Guid gSessionid = new Guid(SessionId);
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                List<decimal> dList = (from c in objPAM2EntitiesContext.SessionResults
                                       where c.ReviewStatus == unProcessedStatusGuiID && c.Reviewer==null && c.SessionId == gSessionid
                                       orderby c.GroupNo ascending
                                       select c.GroupNo).Distinct().ToList<decimal>();
                return dList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool AssignSessionResultsToUser(string SessionId, string groupNo, string PAMUserId, Guid reviewStatusGuid,Guid unProcessedStatusGuiID)
        {
            bool result = false;
            List<SessionResult> sessionResults = null;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                decimal groupNoDec = new decimal(Convert.ToDouble(groupNo));
                sessionResults = objPAM2EntitiesContext.SessionResults.Where(p => p.SessionId == new Guid(SessionId) && p.GroupNo == groupNoDec && p.Reviewer == null && p.ReviewStatus==unProcessedStatusGuiID  ).ToList();

                if (sessionResults != null && sessionResults.Count > 0)
                {
                    result = true;
                    for (int i = 0; i < sessionResults.Count; i++)
                    {
                        sessionResults[i].Reviewer = new Guid(PAMUserId);
                        sessionResults[i].PunchIn = DateTime.UtcNow;
                        if (reviewStatusGuid !=null && reviewStatusGuid != Guid.Empty)
                        {
                            sessionResults[i].ReviewStatus = reviewStatusGuid;
                        }
                        sessionResults[i].CurentStatusDateTime = DateTime.UtcNow;
                        objPAM2EntitiesContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public bool DeAssignSessionResultsToUser(string SessionId, string groupNo, string PAMUserId, string unProcessdStatusVal)
        {
            List<SessionResult> sessionResults = null;
            bool result = false;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                decimal groupNoDec = new decimal(Convert.ToDouble(groupNo));
                sessionResults = objPAM2EntitiesContext.SessionResults.Where(p => p.SessionId == new Guid(SessionId) && p.GroupNo == groupNoDec && p.Reviewer == new Guid(PAMUserId)).ToList();
                if (sessionResults != null && sessionResults.Count > 0)
                {
                    result = true;
                    for (int i = 0; i < sessionResults.Count; i++)
                    {
                        sessionResults[i].Reviewer = null;
                        sessionResults[i].PunchIn = null;
                        var unProcessedStatus = objPAM2EntitiesContext.Status.Where(status => status.Enum.Equals(unProcessdStatusVal)).FirstOrDefault();
                        if (unProcessedStatus != null)
                        {
                            sessionResults[i].ReviewStatus = unProcessedStatus.StatusId;
                        }
                        sessionResults[i].CurentStatusDateTime = null;
                        objPAM2EntitiesContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public SessionResult UpdateSessionResultForReview(string sessionResultId, string reviewStatus, bool isPrimary, DateTime punchoutDate, DateTime curentStatusDateTime)
        {
            SessionResult objSessionResult = null;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSessionResult = objPAM2EntitiesContext.SessionResults.Where(p => p.SessionResultId == new Guid(sessionResultId)).SingleOrDefault();

                if (objSessionResult != null)
                {
                    objSessionResult.IsPrimary = isPrimary;
                    objSessionResult.PunchOut = DateTime.UtcNow;
                    var foundReviewStatus = objPAM2EntitiesContext.Status.Where(status => status.Enum.Equals(reviewStatus)).FirstOrDefault();
                    if (foundReviewStatus != null)
                    {
                        objSessionResult.ReviewStatus = foundReviewStatus.StatusId;
                    }
                    objSessionResult.CurentStatusDateTime = DateTime.UtcNow;
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objSessionResult;
        }

        public void UpdateSessionResultReviewStatus(string sessionResultId, string ReviewStatus, string pamUserId)
        {
            SessionResult objSessionResult = null;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSessionResult = objPAM2EntitiesContext.SessionResults.Where(p => p.SessionResultId == new Guid(sessionResultId)).SingleOrDefault();

                if (objSessionResult != null)
                {
                    Status objStatus = objPAM2EntitiesContext.Status.Where(p => p.StatusId == objSessionResult.ReviewStatus).SingleOrDefault();
                    if (objStatus != null)
                    {
                        if (Convert.ToString(objStatus.Enum) == "300")  // check for auto aceepted if it is then do not update the status
                        {
                            return;
                        }
                    }

                    objSessionResult.ReviewStatus = new Guid(ReviewStatus);
                    objSessionResult.CurentStatusDateTime = DateTime.UtcNow;
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public SessionResult UpdateSessionResultValidGroupStatus(string sessionResultId, bool ValidGroup)
        {
            SessionResult objSessionResult = null;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSessionResult = objPAM2EntitiesContext.SessionResults.Where(p => p.SessionResultId == new Guid(sessionResultId)).SingleOrDefault();

                if (objSessionResult != null)
                {
                    objSessionResult.ValidGroup = ValidGroup;
                    if (!ValidGroup)
                    {
                        objSessionResult.PunchIn = null;
                        objSessionResult.PunchOut = null;
                        objSessionResult.Reviewer = null;
                    }
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objSessionResult;
        }

        public List<SessionResult> GetUserQueue(string sessionId, string pamUserId)
        {
            List<SessionResult> lstSessionResults = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                Guid statusId = pam2EntitiesContext.Status.Where(status => status.Enum == "800").FirstOrDefault().StatusId;
                Guid statusIdAutoPromoted = pam2EntitiesContext.Status.Where(status => status.Enum == "AP").FirstOrDefault().StatusId;
                Guid statusIdAutoFilled = pam2EntitiesContext.Status.Where(status => status.Enum == "AF").FirstOrDefault().StatusId;
                Guid statusIdAutoPromotedFilled = pam2EntitiesContext.Status.Where(status => status.Enum == "APF").FirstOrDefault().StatusId;

                if (statusId != null && statusId != Guid.Empty)
                {
                    lstSessionResults = (from u in pam2EntitiesContext.SessionResults
                                         where u.SessionId == new Guid(sessionId)
                                              && (u.ReviewStatus == statusId || u.ReviewStatus == statusIdAutoPromoted
                                              || u.ReviewStatus == statusIdAutoFilled || u.ReviewStatus == statusIdAutoPromotedFilled)
                                              && u.Reviewer == new Guid(pamUserId)
                                         && u.IsPrimary == true
                                         orderby u.GroupRank descending, u.GroupNo ascending
                                         select u).ToList();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;
        }

        public int FetchSessionGroupCount(string sessionId)
        {
            int? totalGroupCount = 0;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                Session session = (from u in pam2EntitiesContext.Sessions
                                   where u.SessionId == new Guid(sessionId)
                                   select u).FirstOrDefault();
                totalGroupCount = session != null ? (session.GroupCount != null ? session.GroupCount : 0) : 0;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return totalGroupCount.Value;
        }

        public bool GetSessionAutoMergeStatus(string sessionId)
        {
            bool? isAutoMergeInProgress = false;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                isAutoMergeInProgress = (from u in pam2EntitiesContext.Sessions
                                         where u.SessionId == new Guid(sessionId)
                                         select u).FirstOrDefault().IsAutoMergeInProgress;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (isAutoMergeInProgress != null)
                return isAutoMergeInProgress.Value;
            else
                return true;
        }

        public bool GetSessionAutoProcessStatus(string sessionId, string Process)
        {
            bool? isAutoPromoteInProgress = false;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                switch (Process)
                {
                    case "AutoPromote":
                        {
                            isAutoPromoteInProgress = (from u in pam2EntitiesContext.Sessions
                                                       where u.SessionId == new Guid(sessionId)
                                                       select u).FirstOrDefault().IsAutoPromoteInProgress;
                            break;
                        }

                    case "AutoFill":
                        {
                            isAutoPromoteInProgress = (from u in pam2EntitiesContext.Sessions
                                                       where u.SessionId == new Guid(sessionId)
                                                       select u).FirstOrDefault().IsAutoFillInProgress;
                            break;
                        }

                    case "AutoPromoteFill":
                        {
                            isAutoPromoteInProgress = (from u in pam2EntitiesContext.Sessions
                                                       where u.SessionId == new Guid(sessionId)
                                                       select u).FirstOrDefault().IsAutoFillAndPromoteInProgress;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (isAutoPromoteInProgress != null)
                return isAutoPromoteInProgress.Value;
            else
                return false;
        }

        public List<Status> GetAllStatus()
        {
            List<Status> lstStatus = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                lstStatus = (from u in pam2EntitiesContext.Status
                             select u).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstStatus;
        }

        public List<SessionResult> FindNextMatchingRecords(string sessionId, string currentUserId, string prevGroupNo, string reqType, bool skipDeferred)
        {
            List<SessionResult> lstSessionResults = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                pam2EntitiesContext.Database.CommandTimeout = 180;
                decimal intPrevGroupNo = Convert.ToDecimal(prevGroupNo);

                var reviewingStatus = pam2EntitiesContext.Status.Where(status => status.Enum == "800").FirstOrDefault();
                var unprocessedStatus = pam2EntitiesContext.Status.Where(status => status.Enum == "UP").FirstOrDefault();
                var pSuppressedStatus = pam2EntitiesContext.Status.Where(status => status.Enum == "500").FirstOrDefault();
                var pAutoPromotedStatus = pam2EntitiesContext.Status.Where(status => status.Enum == "AP").FirstOrDefault();
                var pAutoFilledStatus = pam2EntitiesContext.Status.Where(status => status.Enum == "AF").FirstOrDefault();
                var pAutoPromotedFilledStatus = pam2EntitiesContext.Status.Where(status => status.Enum == "APF").FirstOrDefault();

                Guid reviewingStatusId = Guid.Empty;
                Guid unprocessedStatusId = Guid.Empty;
                Guid pSuppressedStatusId = Guid.Empty;
                Guid pAutoPromotedStatusId = Guid.Empty;
                Guid pAutoFilledStatusId = Guid.Empty;
                Guid pAutoPromotedFilledStatusId = Guid.Empty;

                if (reviewingStatus != null)
                {
                    reviewingStatusId = reviewingStatus.StatusId;
                }

                if (unprocessedStatus != null)
                {
                    unprocessedStatusId = unprocessedStatus.StatusId;
                }

                if (pSuppressedStatus != null)
                {
                    pSuppressedStatusId = pSuppressedStatus.StatusId;
                }

                if (pAutoPromotedStatus != null)
                {
                    pAutoPromotedStatusId = pAutoPromotedStatus.StatusId;
                }

                if (pAutoFilledStatus != null)
                {
                    pAutoFilledStatusId = pAutoFilledStatus.StatusId;
                }

                if (pAutoPromotedFilledStatus != null)
                {
                    pAutoPromotedFilledStatusId = pAutoPromotedFilledStatus.StatusId;
                }
                
                if (reqType == "FETCH")
                {
                    lstSessionResults = (from u in pam2EntitiesContext.SessionResults
                                         where u.SessionId == new Guid(sessionId) && u.GroupNo > intPrevGroupNo && (u.ValidGroup == null || u.ValidGroup == true)
                                         orderby u.GroupRank descending, u.GroupNo ascending
                                         select u).ToList();
                }
                else
                {
                    lstSessionResults = (from u in pam2EntitiesContext.SessionResults
                                         where u.SessionId == new Guid(sessionId) && u.GroupNo == intPrevGroupNo && (u.ValidGroup == null || u.ValidGroup == true)
                                         orderby u.GroupRank descending, u.GroupNo ascending
                                         select u).ToList();
                }

                if (lstSessionResults != null && lstSessionResults.Count > 0)
                {
                    lstSessionResults = (from u in lstSessionResults
                                         where (u.ReviewStatus == null || u.ReviewStatus == reviewingStatusId || u.ReviewStatus == unprocessedStatusId ||
                                             u.ReviewStatus == pSuppressedStatusId || u.ReviewStatus == pAutoPromotedStatusId || u.ReviewStatus == pAutoFilledStatusId 
                                             || u.ReviewStatus == pAutoPromotedFilledStatusId
                                             )
                                         orderby u.GroupRank descending, u.GroupNo ascending
                                         select u).ToList();
                }

                if (lstSessionResults != null && lstSessionResults.Count > 0)
                {
                    if (skipDeferred && decimal.Equals(0.0, intPrevGroupNo))
                    {
                        lstSessionResults = (from u in lstSessionResults
                                             where u.Reviewer == new Guid(currentUserId)
                                             orderby u.GroupRank descending, u.GroupNo ascending
                                             select u).ToList();
                    }
                    else
                    {
                        if (skipDeferred)
                        {
                            lstSessionResults = (from u in lstSessionResults
                                                 where u.Reviewer == null
                                                 orderby u.GroupRank descending, u.GroupNo ascending
                                                 select u).ToList();
                        }
                        else
                        {
                            lstSessionResults = (from u in lstSessionResults
                                                 where u.Reviewer == new Guid(currentUserId) || u.Reviewer == null
                                                 orderby u.GroupRank descending, u.GroupNo ascending
                                                 select u).ToList();
                        }
                    }
                }

                if (lstSessionResults != null && lstSessionResults.Count > 0)
                {
                    if (skipDeferred)
                    {
                        lstSessionResults = (from u in lstSessionResults
                                             //where u.SessionId == new Guid(sessionId) && u.GroupNo == intPrevGroupNo
                                             //orderby u.PunchIn descending, u.GroupRank descending, u.GroupNo ascending
                                             orderby u.PunchIn descending, u.GroupRank descending, u.GroupNo ascending
                                             select u).ToList();
                    }
                    else
                    {
                        lstSessionResults = (from u in lstSessionResults
                                             // where u.SessionId == new Guid(sessionId) && u.GroupNo == intPrevGroupNo
                                             orderby u.GroupRank descending, u.GroupNo ascending
                                             select u).ToList();
                    }
                }

                if (lstSessionResults != null && lstSessionResults.Count > 0)
                {
                    SessionResult sessionResult = null;
                    sessionResult = lstSessionResults[0];
                    if (sessionResult != null)
                    {
                        lstSessionResults = (from u in pam2EntitiesContext.SessionResults
                                             where u.SessionId == sessionResult.SessionId
                                             && u.GroupNo == sessionResult.GroupNo
                                             && (u.ValidGroup == null || u.ValidGroup == true)
                                             && (u.ReviewStatus == null || u.ReviewStatus == reviewingStatusId || u.ReviewStatus == unprocessedStatusId ||
                                             u.ReviewStatus == pSuppressedStatusId || u.ReviewStatus == pAutoPromotedStatusId
                                             || u.ReviewStatus == pAutoFilledStatusId 
                                             || u.ReviewStatus == pAutoPromotedFilledStatusId)
                                             orderby u.IsPrimary descending, u.GroupRank descending, u.GroupNo ascending
                                             select u).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;

        }

        public void UpdateSessionResultRecordAsPrimary(string SessionResultID)
        {
            try
            {
                Guid gSessionResultID = new Guid(SessionResultID);

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                SessionResult objSessionResultTobePrimary = (from c in pam2EntitiesContext.SessionResults
                                                         where c.SessionResultId == gSessionResultID 
                                                  select c).FirstOrDefault<SessionResult>();

                Guid gSessionID = objSessionResultTobePrimary.SessionId;
                decimal dGroupNo = objSessionResultTobePrimary.GroupNo;

                SessionResult objSessionResult = (from c in pam2EntitiesContext.SessionResults
                                                  where c.SessionId == gSessionID && c.IsPrimary == true
                                                  && c.GroupNo == dGroupNo
                                                  select c).FirstOrDefault<SessionResult>();
                if (objSessionResult != objSessionResultTobePrimary)
                {
                    objSessionResult.IsPrimary = false;
                    objSessionResultTobePrimary.AutoPromote = true;
                    pam2EntitiesContext.SaveChanges();

                    objSessionResultTobePrimary = (from c in pam2EntitiesContext.SessionResults
                                                   where c.SessionResultId == gSessionResultID
                                                   select c).FirstOrDefault<SessionResult>();

                    objSessionResultTobePrimary.IsPrimary = true;
                    objSessionResultTobePrimary.AutoPromote = true;
                    pam2EntitiesContext.SaveChanges();

                    List<SessionResult> lstSessionResult = (from c in pam2EntitiesContext.SessionResults
                                                            where c.SessionId == gSessionID &&
                                                            (c.AutoPromote.Value == false || c.AutoPromote == null)
                                                             && c.GroupNo == dGroupNo
                                                            select c).ToList<SessionResult>();
                    foreach (SessionResult rec in lstSessionResult)
                    {
                        var obj = (from c in pam2EntitiesContext.SessionResults
                                   where c.SessionResultId == rec.SessionResultId
                                   select c).FirstOrDefault<SessionResult>();
                        obj.AutoPromote = true;
                        pam2EntitiesContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<PAM1Attribute> GetSessionDisplaySettingForReview(string entitySettingId, string sessionId)
        {
            List<PAM1Attribute> lstPam1Attributes = new List<PAM1Attribute>();
            try
            {
                MatchGroupAttributeSettingResultSet sessionDisplaySetting = GetSessionSectionsAttributeSettings(entitySettingId, sessionId);
                if (sessionDisplaySetting != null && sessionDisplaySetting.MatchGroupAttributeSettings != null && sessionDisplaySetting.MatchGroupAttributeSettings.Count > 0)
                {
                    string entityName = "";

                    EntitySettingResultSet entitySetting = GetEntitySettings();
                    if (entitySetting != null && entitySetting.EntitySettings != null && entitySetting.EntitySettings.Count > 0)
                    {
                        var entitySettingObj = (from setting in entitySetting.EntitySettings
                                                where setting.EntitySettingId.ToLower() == entitySettingId.ToLower()
                                                select setting
                                                    ).FirstOrDefault();
                        if (entitySettingObj != null)
                        {

                            entityName = entitySettingObj.EntityLogicalName.ToLower();

                        }
                    }
                    string sectionName = "";
                    int sectionOrder = 0;

                    PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                    List<Section> AllSections = (from sectionSetting in pam2EntitiesContext.Sections
                                                 select sectionSetting).ToList();

                    foreach (var setting in sessionDisplaySetting.MatchGroupAttributeSettings)
                    {
                        if (setting != null && setting.children != null && setting.children.Count > 0)
                        {
                            foreach (var attributeSetting in setting.children)
                            {
                                // Added the following line to show only those fields which has set 'Show On PAM' is true in Disppay settings
                                if (!Convert.ToBoolean(attributeSetting.IsVisible))
                                    continue;

                                sectionName = "Default";
                                sectionOrder = 0;
                                PAM1Attribute attribute = new PAM1Attribute();
                                attribute.Id = attributeSetting.MatchAttributeSettingId;
                                attribute.FieldName = attributeSetting.SchemaName.ToLower();
                                attribute.Value = "!#!Value!#!";
                                attribute.Type = "!#!Type!#!";
                                attribute.Entity = entityName;
                                if (AllSections != null && AllSections.Count > 0 && !string.IsNullOrWhiteSpace(setting.SectionId))
                                {
                                    var section = (from sectionSetting in AllSections
                                                   where sectionSetting.SectionId == new Guid(setting.SectionId)
                                                   select sectionSetting).FirstOrDefault();

                                    if (section != null)
                                    {
                                        sectionOrder = section.DisplayOrder;
                                        sectionName = section.SectionName;
                                    }
                                }
                                attribute.SectionDisplayOrder = sectionOrder;
                                attribute.GroupName = sectionName;
                                attribute.CustomName = attributeSetting.CustomName;
                                attribute.ActualValue = "";
                                attribute.DisplayOrder = Convert.ToInt32(attributeSetting.DisplayOrder);
                                attribute.DisplayName = attributeSetting.DisplayName;
                                attribute.IsHidden = !(attributeSetting.IsVisible == null ? true : Convert.ToBoolean(attributeSetting.IsVisible));
                                attribute.HandleManually = (attributeSetting.ExcludeUpdate == null ? false : Convert.ToBoolean(attributeSetting.ExcludeUpdate));
                                lstPam1Attributes.Add(attribute);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstPam1Attributes;
        }

        public UserResultSet GetUserInfoForReview(string pamUserId)
        {
            UserResultSet userResultSet = null;
            List<PAMUser> lstUsers = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(pamUserId))
                {
                    PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                    lstUsers = (from u in pam2EntitiesContext.Users
                                where u.UserId == new Guid(pamUserId)
                                select u).AsEnumerable().Select(user => new PAMUser
                                {
                                    CRMUserId = user.CRMUserId,
                                    SkipDeferred = user.SkipDeferred,
                                    GridHeight = user.GridHeight,
                                    GridWidth = user.GridWidth,
                                    UserId = user.UserId
                                }).ToList<PAMUser>();

                    if (lstUsers != null && lstUsers.Count > 0)
                    {

                        userResultSet = new UserResultSet();
                        userResultSet.Result = true;
                        userResultSet.success = true;
                        userResultSet.Users = lstUsers;
                        userResultSet.total = lstUsers.Count;
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return userResultSet;
        }

        public bool AddReviewerUserRole(string pamUserId)
        {
            bool success = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(pamUserId))
                {
                    PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                    Role reviewerRole = pam2EntitiesContext.Roles.Where(role => role.Name.ToLower() == "reviewer").FirstOrDefault();

                    if (reviewerRole != null && reviewerRole.RoleId != null && reviewerRole.RoleId != Guid.Empty)
                    {
                        UserRole foundUserRole = null;

                        foundUserRole = pam2EntitiesContext.UserRoles.Where(userRole => userRole.RoleId == reviewerRole.RoleId && userRole.UserId == new Guid(pamUserId)).FirstOrDefault();
                        if (foundUserRole != null)
                        {
                            foundUserRole.UserId = new Guid(pamUserId);
                            foundUserRole.RoleId = reviewerRole.RoleId;
                            pam2EntitiesContext.SaveChanges();
                            success = true;
                        }
                        else
                        {
                            UserRole newUserRole = new UserRole();
                            newUserRole.RoleId = reviewerRole.RoleId;
                            newUserRole.UserId = new Guid(pamUserId);
                            pam2EntitiesContext.UserRoles.Add(newUserRole);
                            pam2EntitiesContext.SaveChanges();
                            success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return success;
        }


        public bool UpdateUserInfo(PAMUser user)
        {
            bool success = false;
            try
            {
                if (user != null && user.UserId != null && user.UserId != Guid.Empty)
                {
                    PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                    User foundUserObj = pam2EntitiesContext.Users.Where(u => u.UserId == user.UserId).FirstOrDefault();
                    if (foundUserObj != null)
                    {
                        foundUserObj.SkipDeferred = user.SkipDeferred;
                        foundUserObj.GridWidth = user.GridWidth;
                        foundUserObj.GridHeight = user.GridHeight;
                        pam2EntitiesContext.SaveChanges();
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return success;

        }

        public void DeleteAttributesDeletedInMSCRM(List<Guid> attributeSettingIds)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                var attributeSettings = pam2EntitiesContext.AttributeSettings.Where(s => attributeSettingIds.Contains(s.AttributeSettingId));

                if (attributeSettings != null && attributeSettings.Count() > 0)
                {
                    var x = pam2EntitiesContext.AttributeSettings.RemoveRange(attributeSettings);
                    int count = pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }
        }

        public List<PAMSessionAutoMergeRule> GetSessionAutoMergeRules(string sessionId, string entitySettingId)
        {
            List<PAMSessionAutoMergeRule> lstSessionAutoMergeRules = new List<PAMSessionAutoMergeRule>();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                var query = from am in pam2EntitiesContext.AutoMergeRuleMasters
                            from ea in am.EntityAutoMergeRules.Where(xrRecord => xrRecord.AutoMergeRuleId == am.AutoMergeRuleId).DefaultIfEmpty()
                            from sa in ea.SessionAutoMergeRules.Where(yyrecord => yyrecord.EntityAutoMergeRuleId == ea.EntityRuleId).DefaultIfEmpty()
                            // where sa.SessionId == new Guid(sessionId)
                            select new
                            {
                                AM = am,
                                EA = ea,
                                SA = sa
                            };

                if (query != null && query.Count() > 0)
                {
                    foreach (var record in query)
                    {
                        if (record.SA != null && record.EA != null && record.AM != null && record.SA.SessionId == new Guid(sessionId))
                        {
                            lstSessionAutoMergeRules.Add(
                                new PAMSessionAutoMergeRule
                                {
                                    EntityRuleId = (record.SA.EntityAutoMergeRuleId != null && record.SA.EntityAutoMergeRuleId != Guid.Empty) ? record.SA.EntityAutoMergeRuleId.ToString() : "",
                                    EntityRuleStatus = record.EA.Status != null ? Convert.ToBoolean(record.EA.Status) : false,
                                    RuleDesc = record.AM.Description,
                                    SessionId = sessionId,
                                    SessionRuleId = record.SA.SessionAutoMergeRuleId != null && record.SA.SessionAutoMergeRuleId != Guid.Empty ? record.SA.SessionAutoMergeRuleId.ToString() : "",
                                    SessionruleStatus = record.SA.Status != null ? Convert.ToBoolean(record.SA.Status) : false
                                });
                        }
                    }
                    if (lstSessionAutoMergeRules.Count <= 0)
                    {
                        var enttiyRecord = (from record in query
                                            where record.EA != null && record.EA.EntitySettingId == new Guid(entitySettingId)
                                            select record).FirstOrDefault();
                        if (enttiyRecord != null)
                        {
                            lstSessionAutoMergeRules.Add(
                                                           new PAMSessionAutoMergeRule
                                                           {
                                                               EntityRuleId = (enttiyRecord.EA.EntityRuleId != null && enttiyRecord.EA.EntityRuleId != Guid.Empty) ? enttiyRecord.EA.EntityRuleId.ToString() : "",
                                                               EntityRuleStatus = enttiyRecord.EA.Status != null ? Convert.ToBoolean(enttiyRecord.EA.Status) : false,
                                                               RuleDesc = enttiyRecord.AM.Description,
                                                               SessionId = sessionId,
                                                               SessionRuleId = (enttiyRecord.EA.EntityRuleId != null && enttiyRecord.EA.EntityRuleId != Guid.Empty) ? enttiyRecord.EA.EntityRuleId.ToString() : "",
                                                               SessionruleStatus = false
                                                           });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstSessionAutoMergeRules;
        }

        public void DeleteSessionAutoMergeRules(List<SessionAutoMergeRule> lstSessionRules)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                var lstSessionRuleIds = (from sessionAutoMergeRule in lstSessionRules
                                         select sessionAutoMergeRule.SessionAutoMergeRuleId).ToList();

                var sessinAutoMergeRules = pam2EntitiesContext.SessionAutoMergeRules.Where(s => lstSessionRuleIds.Contains(s.SessionAutoMergeRuleId));
                if (sessinAutoMergeRules != null && sessinAutoMergeRules.Count() > 0)
                {
                    var x = pam2EntitiesContext.SessionAutoMergeRules.RemoveRange(sessinAutoMergeRules);
                    int count = pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void AddSessionAutoMergeRule(List<SessionAutoMergeRule> lstSessionRules)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                DeleteSessionAutoMergeRules(lstSessionRules);
                var x = pam2EntitiesContext.SessionAutoMergeRules.AddRange(lstSessionRules);
                int count = pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Session UpdateSessionStatus(string sessionId, string sessionStatus)
        {
            Session objSession = null;
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSession = objPAM2EntitiesContext.Sessions.Where(p => p.SessionId == new Guid(sessionId)).SingleOrDefault();
                if (objSession != null)
                {
                    objSession.Status = objPAM2EntitiesContext.Status.Where(status => status.Enum.Equals(sessionStatus)).FirstOrDefault();
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objSession;
        }

        public List<SessionAutoMergeRule> GetActiveSessionRules(string sessionId)
        {
            List<SessionAutoMergeRule> lstSessionAutoMergeRules = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");

                lstSessionAutoMergeRules = (from rule in pam2EntitiesContext.SessionAutoMergeRules
                                            where rule.SessionId == new Guid(sessionId)
                                            select rule).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionAutoMergeRules;
        }

        public List<SessionResult> GetAllPrimarySessionResults(string sessionId, string pamUserId, string reviewingStatusEnum, string declinedStatusEnum)
        {
            List<SessionResult> lstPrimarySessionResults = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                //  int  reviewingStatusVal = Convert.ToInt32(Convert.ChangeType(Enums.REVIEWSTATUS.ACCEPTED, Enums.REVIEWSTATUS.ACCEPTED.GetTypeCode()));
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);

                Status unprocessedReviewStatus = pam2EntitiesContext.Status.Where(st => st.Enum == "UP").FirstOrDefault();
                if (unprocessedReviewStatus != null)
                {
                    lstReviewStatus.Add(unprocessedReviewStatus.StatusId);
                }


                Status reviewStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum).FirstOrDefault();
                if (reviewStatus != null)
                {
                    lstReviewStatus.Add(reviewStatus.StatusId);
                }

                Status declinedStatus = pam2EntitiesContext.Status.Where(st => st.Enum == declinedStatusEnum).FirstOrDefault();
                if (declinedStatus != null)
                {
                    lstReviewStatus.Add(declinedStatus.StatusId);
                }
                var query = pam2EntitiesContext.SessionResults.Where(s => ((s.Reviewer == new Guid(pamUserId) || s.Reviewer == null) && (s.SessionId == new Guid(sessionId) && s.IsPrimary == true) && (lstReviewStatus.Contains(s.ReviewStatus)))).OrderByDescending(s => s.GroupRank).ThenBy(y => y.GroupNo);//.ThenByDescending(x => x.IsPrimary);
                if (query != null)
                {
                    lstPrimarySessionResults = query.ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstPrimarySessionResults;
        }

        public List<SessionResult> GetAllPrimarySessionResultsForAutoPromote(string sessionId, string pamUserId)
        {
            List<SessionResult> lstPrimarySessionResults = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                //  int  reviewingStatusVal = Convert.ToInt32(Convert.ChangeType(Enums.REVIEWSTATUS.ACCEPTED, Enums.REVIEWSTATUS.ACCEPTED.GetTypeCode()));
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);

                //Status unprocessedReviewStatus = pam2EntitiesContext.Status.Where(st => st.Enum == "UP").FirstOrDefault();
                //if (unprocessedReviewStatus != null)
                //{
                //    lstReviewStatus.Add(unprocessedReviewStatus.StatusId);
                //}


                //Status reviewStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum).FirstOrDefault();
                //if (reviewStatus != null)
                //{
                //    lstReviewStatus.Add(reviewStatus.StatusId);
                //}

                //Status declinedStatus = pam2EntitiesContext.Status.Where(st => st.Enum == declinedStatusEnum).FirstOrDefault();
                //if (declinedStatus != null)
                //{
                //    lstReviewStatus.Add(declinedStatus.StatusId);
                //}
                var query = pam2EntitiesContext.SessionResults.Where(s => ((s.Reviewer == new Guid(pamUserId) || s.Reviewer == null) &&
                    (s.SessionId == new Guid(sessionId) && s.IsPrimary == true) && s.ValidGroup == true)).OrderByDescending(s => s.GroupRank).ThenBy(y => y.GroupNo);//.ThenByDescending(x => x.IsPrimary);
                if (query != null)
                {
                    lstPrimarySessionResults = query.ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstPrimarySessionResults;
        }

        public Session SetSessionAutoMergeStatus(string sessionId, bool sessionStatus, out string sessionName)
        {
            Session objSession = null;
            sessionName = "";
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSession = objPAM2EntitiesContext.Sessions.Where(p => p.SessionId == new Guid(sessionId)).SingleOrDefault();
                if (objSession != null)
                {
                    objSession.IsAutoMergeInProgress = sessionStatus;
                    objSession.RefreshDate = DateTime.UtcNow;
                    sessionName = objSession.SessionName;
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objSession;
        }

        public Session SetSessionAutoPromoteAndFillStatus(string sessionId, bool sessionStatus,string Process, out string sessionName)
        {
            Session objSession = null;
            sessionName = "";
            try
            {
                PAM2Entities objPAM2EntitiesContext = new PAM2Entities(sqlConnString);
                objSession = objPAM2EntitiesContext.Sessions.Where(p => p.SessionId == new Guid(sessionId)).SingleOrDefault();

                if (objSession != null)
                {
                    if(Process.ToLower().Trim() == "autopromote")
                    {
                        objSession.IsAutoPromoteInProgress = sessionStatus;
                        if(!sessionStatus)
                        {
                            objSession.IsAutoPromotedAll = true;
                        }
                    }

                    if (Process.ToLower().Trim() == "autofill")
                    {
                        objSession.IsAutoFillInProgress = sessionStatus;
                        if (!sessionStatus)
                        {
                            objSession.IsAutoFillAll = true;
                        }
                    }

                    if (Process.ToLower().Trim() == "autopromotefill")
                    {
                        objSession.IsAutoFillAndPromoteInProgress = sessionStatus;
                        if (!sessionStatus)
                        {
                            objSession.IsAutoPromoteFillAll = true;
                        }
                    }

                    objSession.RefreshDate = DateTime.UtcNow;
                    sessionName = objSession.SessionName;
                    objPAM2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objSession;
        }

         public ResultSet AutoAcceptAlltheAutoRecords(string SessionId, Status AutoAccepted, Status AutoPromoted,
           Status AutoFill, Status AutoPromotedFill, string PAMUserId)
        {
            ResultSet objResultSet = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                 List<SessionResult> lstSessionResultAutoPromoted = (from c in pam2EntitiesContext.SessionResults
                                                                where c.ReviewStatus == AutoPromoted.StatusId ||
                                                                    c.ReviewStatus == AutoFill.StatusId ||
                                                                    c.ReviewStatus == AutoPromotedFill.StatusId
                                                                select c).ToList<SessionResult>();

                foreach (var sessionResult in lstSessionResultAutoPromoted)
                {
                    SessionResult o = (from c in pam2EntitiesContext.SessionResults
                                       where c.SessionResultId == sessionResult.SessionResultId
                                       select c).FirstOrDefault();
                    sessionResult.ReviewStatus = AutoAccepted.StatusId;
                    sessionResult.CurentStatusDateTime = DateTime.UtcNow;
                    pam2EntitiesContext.SaveChanges();
                }

                objResultSet.success = true;
                objResultSet.Result = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return objResultSet;
        }

         public ResultSet CheckSessionAutoStatus(string SessionId)
         {
             ResultSet objResultSet = new ResultSet();
             try
             {
                 Guid gSessionid = new Guid(SessionId);
                 PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                 Session objSession = (from c in pam2EntitiesContext.Sessions
                                       where c.SessionId == gSessionid
                                       select c).FirstOrDefault<Session>();
                 //if (objSession == null)
                 //    objResultSet.Result = false;
                 //else
                 //    objResultSet.Result = true;

                 if (Convert.ToBoolean(objSession.IsAutoPromoteFillAll))
                 {
                     objResultSet.Message = "AutoPromoteFill";
                     objResultSet.Result = true;
                 }
                 else if (Convert.ToBoolean(objSession.IsAutoFillAll))
                 {
                     objResultSet.Message = "AutoFill";
                     objResultSet.Result = true;
                 }
                 else if (Convert.ToBoolean(objSession.IsAutoPromotedAll))
                 {
                     objResultSet.Message = "AutoPromote";
                     objResultSet.Result = true;
                 }
                 else
                 {
                     objResultSet.Result = false;
                 }

                 objResultSet.success = true;

             }
             catch (Exception ex)
             {
                 throw ex;
             }
             return objResultSet;
         }

        public void AddAutoMergeLog(string entitSettingId, string sessionId, string sessionName, DateTime startDateTime, DateTime endDateTime,
            string rules, int recordsProcessed, string loggedInUserId, string groupNo = "")
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                AutoMergeLogDetail log = new AutoMergeLogDetail();
                log.AppliedAutoMergeRules = rules;
                log.EndDateTime = endDateTime;
                log.EntitySettingId = new Guid(entitSettingId);
                log.LoggedInUser = new Guid(loggedInUserId);
                log.RecordsProcessed = recordsProcessed;
                log.SessionId = new Guid(sessionId);
                log.SessionName = sessionName;
                log.StartDateTime = startDateTime;
                if (!string.IsNullOrWhiteSpace(groupNo))
                {
                    decimal groupNoDecimal = new decimal(0.0);
                    decimal.TryParse(groupNo, out groupNoDecimal);
                    log.GroupNo = groupNoDecimal;
                }
                pam2EntitiesContext.AutoMergeLogDetails.Add(log);
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void AddAutoPromoteAndFillLog(string entitSettingId, string sessionId, string sessionName, DateTime startDateTime,
            DateTime endDateTime, string Process, int recordsProcessed, string loggedInUserId, string groupNo = "")
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                AutoPromoteAndFillLogDetail log = new AutoPromoteAndFillLogDetail();
                log.AutoPromoteAndFillID = Guid.NewGuid();
                log.Process = Process;
                log.EndDateTime = endDateTime;
                log.EntitySettingId = new Guid(entitSettingId);
                log.LoggedInUser = new Guid(loggedInUserId);
                log.RecordsProcessed = recordsProcessed;
                log.SessionId = new Guid(sessionId);
                log.StartDateTime = startDateTime;
                if (!string.IsNullOrWhiteSpace(groupNo))
                {
                    decimal groupNoDecimal = new decimal(0.0);
                    decimal.TryParse(groupNo, out groupNoDecimal);
                    log.GroupNo = groupNoDecimal;
                }
                pam2EntitiesContext.AutoPromoteAndFillLogDetails.Add(log);
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void MarkAutoMergeReviewStatus(string sessionresultId, string pamUserId, string autoMergeStatusEnum)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                SessionResult sessionResult = pam2EntitiesContext.SessionResults.Where(s => s.SessionResultId == new Guid(sessionresultId)).FirstOrDefault();
                if (sessionResult != null)
                {
                    sessionResult.Reviewer = new Guid(pamUserId);
                    sessionResult.PunchIn = DateTime.UtcNow;
                    sessionResult.PunchOut = DateTime.UtcNow;
                    Status autoMergeStatus = pam2EntitiesContext.Status.Where(x => x.Enum == autoMergeStatusEnum).FirstOrDefault();
                    if (autoMergeStatus != null)
                        sessionResult.ReviewStatus = autoMergeStatus.StatusId;
                    sessionResult.CurentStatusDateTime = DateTime.UtcNow;
                    pam2EntitiesContext.SaveChanges();

                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public SessionResult GetSessionResultForAutoMerge(string sessionResultId)
        {
            SessionResult result = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                result = pam2EntitiesContext.SessionResults.Where(s => s.SessionResultId == new Guid(sessionResultId)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public int GetGroupWiseSessionResultCount(string sessionId, decimal groupNo)
        {
            int groupCount = 0;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                var result = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo);
                if (result != null && result.Count() > 0)
                {
                    groupCount = result.Count();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return groupCount;
        }

        public int GetGroupWisePermanantlySuppressedSessionResultCount(string sessionId, decimal groupNo, string permanantlySuppressedEnum)
        {
            int groupCount = 0;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                Status permanantlySuppressedStatus = pam2EntitiesContext.Status.Where(x => x.Enum == permanantlySuppressedEnum).FirstOrDefault();
                var result = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.ReviewStatus == permanantlySuppressedStatus.StatusId);
                if (result != null && result.Count() > 0)
                {
                    groupCount = result.Count();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return groupCount;
        }

        public List<SessionResult> GetSessionResultForAutoMerge(string sessionId, decimal groupNo, string reviewingStatusEnum, string declinedStatusEnum, bool checkForSuppressedRecords)
        {
            List<SessionResult> lstPrimarySessionResults = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                //  int  reviewingStatusVal = Convert.ToInt32(Convert.ChangeType(Enums.REVIEWSTATUS.ACCEPTED, Enums.REVIEWSTATUS.ACCEPTED.GetTypeCode()));
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);

                Status unprocessedReviewStatus = pam2EntitiesContext.Status.Where(st => st.Enum == "UP").FirstOrDefault();
                if (unprocessedReviewStatus != null)
                {
                    lstReviewStatus.Add(unprocessedReviewStatus.StatusId);
                }

                Status reviewStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum).FirstOrDefault();
                if (reviewStatus != null)
                {
                    lstReviewStatus.Add(reviewStatus.StatusId);
                }

                Status declinedStatus = pam2EntitiesContext.Status.Where(st => st.Enum == declinedStatusEnum).FirstOrDefault();
                if (declinedStatus != null)
                {
                    lstReviewStatus.Add(declinedStatus.StatusId);
                }

                if (!checkForSuppressedRecords)
                {
                    var query = pam2EntitiesContext.SessionResults.Where(s => (s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && (lstReviewStatus.Contains(s.ReviewStatus)))).OrderByDescending(s => s.IsPrimary).ThenByDescending(y => y.GroupRank).ThenByDescending(x => x.GroupNo);
                    if (query != null)
                    {
                        lstPrimarySessionResults = query.ToList();
                    }
                }
                else
                {
                    var query = pam2EntitiesContext.SessionResults.Where(s => (s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo)).OrderByDescending(s => s.IsPrimary).ThenByDescending(y => y.GroupRank).ThenByDescending(x => x.GroupNo);
                    if (query != null)
                    {
                        lstPrimarySessionResults = query.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstPrimarySessionResults;
        }

        public void AddSuppressionHistoryRecord(List<SuppressionHstory> lstHistoryRecords)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                foreach (var record in lstHistoryRecords)
                {
                    if (!string.IsNullOrWhiteSpace(record.PrimaryId.ToString()) && !string.IsNullOrWhiteSpace(record.SecondaryId.ToString()) && record.ReviewedBy != null)
                    {
                        string suppressKey = GetSuppressKey(record.PrimaryId.ToString(), record.SecondaryId.ToString());
                        if (!UpdateSuppressionHistory(record.SuppressKey, record.ReviewedBy))
                        {
                            record.SuppressKey = suppressKey;
                            pam2EntitiesContext.SuppressionHstories.Add(record);
                            pam2EntitiesContext.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private bool UpdateSuppressionHistory(string suppressKey, Guid userId)
        {
            SuppressionHstory suppressHistoryRecord = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                suppressHistoryRecord = pam2EntitiesContext.SuppressionHstories.Where(s => s.SuppressKey == suppressKey).FirstOrDefault();
                if (suppressHistoryRecord != null)
                {
                    suppressHistoryRecord.UpdatedDate = DateTime.UtcNow;
                    suppressHistoryRecord.UpdatedBy = userId;
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return suppressHistoryRecord != null ? true : false;
        }

        private string GetSuppressKey(string strPrimaryId, string strSecondaryId)
        {
            string suppressKey = "";
            try
            {
                if (string.Compare(strPrimaryId.TrimStart('{').TrimEnd('}'), strSecondaryId.TrimStart('{').TrimEnd('}')) <= 0)
                {
                    suppressKey = strPrimaryId + strSecondaryId;
                }
                else
                {
                    suppressKey = strSecondaryId + strPrimaryId;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return suppressKey;
        }

        public void ReleaseGroup(string sessionId, decimal groupNo, string userId, string reviewingStatusEnum,
            string declinedStatusEnum, string pSuppressedStatusEnum, string unprocessedStatusEnum,
            string[] AutoStatusEnums, string userNotes)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);

                string s1 = String.Empty, s2 = String.Empty, s3 = String.Empty;
                if (AutoStatusEnums.Length == 3)
                {
                    s1 = AutoStatusEnums[0];
                    s2 = AutoStatusEnums[1];
                    s3 = AutoStatusEnums[2];
                }

                List<Status> lstStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum || st.Enum == declinedStatusEnum ||
                    st.Enum == pSuppressedStatusEnum || st.Enum == unprocessedStatusEnum || st.Enum == s1 || st.Enum == s2 || st.Enum == s3).ToList();
                if (lstStatus != null && lstStatus.Count > 0)
                {
                    lstStatus.ForEach(st => lstReviewStatus.Add(st.StatusId));
                }
                var query = pam2EntitiesContext.SessionResults.Where(s => ((s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.Reviewer == new Guid(userId)) && (lstReviewStatus.Contains(s.ReviewStatus))));
                if (query != null && query.Count() > 0)
                {
                    Status UnProcessedStatus = lstStatus.Where(x => x.Enum == unprocessedStatusEnum).FirstOrDefault();
                    if (UnProcessedStatus != null)
                    {
                        query.ToList().ForEach(x => { x.ReviewStatus = UnProcessedStatus.StatusId; x.Reviewer = null; x.PunchIn = null; x.CurentStatusDateTime = DateTime.UtcNow; if (Convert.ToBoolean(x.IsPrimary)) { x.UserNotes = userNotes; } });
                        pam2EntitiesContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public void AssignGroup(string sessionId, decimal groupNo, string userId,
            string selectedUserId, string reviewingStatusEnum, string declinedStatusEnum,
            string pSuppressedStatusEnum, string unprocessedStatusEnum, string[] AutoStatusEnums,
            string userNotes)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);

                string s1 = String.Empty, s2 = String.Empty, s3 = String.Empty;
                if (AutoStatusEnums.Length == 3)
                {
                    s1 = AutoStatusEnums[0];
                    s2 = AutoStatusEnums[1];
                    s3 = AutoStatusEnums[2];
                }

                List<Status> lstStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum ||
                    st.Enum == declinedStatusEnum || st.Enum == pSuppressedStatusEnum || st.Enum == unprocessedStatusEnum 
                    || st.Enum == s1 || st.Enum == s2 || st.Enum == s3).ToList();
                if (lstStatus != null && lstStatus.Count > 0)
                {
                    lstStatus.ForEach(st => lstReviewStatus.Add(st.StatusId));
                    var query = pam2EntitiesContext.SessionResults.Where(s => ((s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.Reviewer == new Guid(userId)) && (lstReviewStatus.Contains(s.ReviewStatus))));
                    if (query != null && query.Count() > 0)
                    {
                        query.ToList().ForEach(x =>
                        {
                            x.ReviewStatus = (x.Status1.Enum == s1 || x.Status1.Enum == s2 || x.Status1.Enum == s3) ? x.ReviewStatus:  lstStatus.Where(st => st.Enum == reviewingStatusEnum).FirstOrDefault().StatusId;
                            x.Reviewer = new Guid(selectedUserId);
                            x.PunchIn = DateTime.UtcNow;
                            x.CurentStatusDateTime = DateTime.UtcNow;
                            if (Convert.ToBoolean(x.IsPrimary))
                            {
                                x.UserNotes = userNotes;
                            }
                        });
                        pam2EntitiesContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public void ChangeSessionMergeStatus(string sessionId, string statusEnum)
        {
            try
            {
                Session sessionObj = null;
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                sessionObj = pam2EntitiesContext.Sessions.Where(s => s.SessionId == new Guid(sessionId)).FirstOrDefault();
                if (sessionObj != null)
                {
                    List<Status> allStatus = GetAllStatus();
                    Status reviewdStatus = null;
                    if (allStatus != null && allStatus.Count > 0)
                    {
                        reviewdStatus = allStatus.Where(st => st.Enum == statusEnum).FirstOrDefault();
                        if (reviewdStatus != null)
                        {
                            sessionObj.MergeStatus = reviewdStatus.StatusId;
                            sessionObj.RefreshDate = DateTime.UtcNow;
                            pam2EntitiesContext.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int GetReviewingAndNullSessionGroupCount(string sessionId, string reviewingStatusEnum)
        {
            int count = 0;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Status> allStatus = GetAllStatus();
                Status reviewdStatus = null;

                Status unprocessedReviewStatus = null;
                Status AutoPromotedStatus = null;
                Status AutoFilledStatus = null;
                Status AutoPromotedFilledStatus = null;

                if (allStatus != null && allStatus.Count > 0)
                {
                    reviewdStatus = allStatus.Where(st => st.Enum == reviewingStatusEnum).FirstOrDefault();
                    unprocessedReviewStatus = allStatus.Where(st => st.Enum == "UP").FirstOrDefault();
                    AutoPromotedStatus = allStatus.Where(st => st.Enum == "AP").FirstOrDefault();
                    AutoFilledStatus = allStatus.Where(st => st.Enum == "AF").FirstOrDefault();
                    AutoPromotedFilledStatus = allStatus.Where(st => st.Enum == "APF").FirstOrDefault();

                    if (reviewdStatus != null)
                        count = pam2EntitiesContext.SessionResults.Count(x => (x.SessionId == new Guid(sessionId) && x.IsPrimary == true) &&
                            (x.ReviewStatus == null || x.ReviewStatus == unprocessedReviewStatus.StatusId ||
                            x.ReviewStatus == reviewdStatus.StatusId || x.ReviewStatus == AutoPromotedStatus.StatusId ||
                            x.ReviewStatus == AutoFilledStatus.StatusId || x.ReviewStatus == AutoPromotedFilledStatus.StatusId
                            ));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return count;
        }

        public int GetSessionAllResultsCount(string sessionId)
        {
            int count = -1;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                count = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId)).Count();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return count;
        }

        public int GetSessionMergedResultsCount(string sessionId, string mergedStatusEnum)
        {
            int count = -1;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Status> allStatus = GetAllStatus();
                Status reviewdStatus = null;
                if (allStatus != null && allStatus.Count > 0)
                {
                    reviewdStatus = allStatus.Where(st => st.Enum == mergedStatusEnum).FirstOrDefault();
                    if (reviewdStatus != null)
                        count = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId) && s.ReviewStatus == reviewdStatus.StatusId).Count();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return count;
        }

        public int GetSessionDeclinedSuppressedAndPSuppressedResultsCount(string sessionId, string declinedStatusEnum, string suppressedStatusEnum, string pSuppressedStatusEnum)
        {
            int count = -1;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Status> allStatus = GetAllStatus();
                if (allStatus != null && allStatus.Count > 0)
                {
                    List<Guid?> lstReviewStatus = new List<Guid?>();
                    List<Status> lstStatus = pam2EntitiesContext.Status.Where(st => st.Enum == declinedStatusEnum || st.Enum == suppressedStatusEnum || st.Enum == pSuppressedStatusEnum).ToList();
                    if (lstStatus != null && lstStatus.Count > 0)
                    {
                        lstStatus.ForEach(st => lstReviewStatus.Add(st.StatusId));
                        if (lstReviewStatus != null && lstReviewStatus.Count > 0)
                            count = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId) && lstReviewStatus.Contains(s.ReviewStatus)).Count();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return count;
        }

        public decimal GetSessionLargestGroupNo(string sessionId)
        {
            decimal groupNo = 0;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                var query = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId)).OrderByDescending(x => x.GroupNo);
                if (query != null && query.Count() > 0)
                {
                    groupNo = Convert.ToDecimal(query.ToList()[0].GroupNo);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return groupNo;
        }

        public List<SessionResult> GetSessionResultBySessionAndGroupNo(string SessionID, decimal GroupNo)
        {
            List<SessionResult> lstSessionResult = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                lstSessionResult = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(SessionID) && s.GroupNo == GroupNo).ToList<SessionResult>();

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstSessionResult;
        }


        #endregion

        #region Suppression Settings

        public void SaveSuppressionSettings(string value, string PAMUserId)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<SuppressionSetting> foundSettings = null;
                foundSettings = pam2EntitiesContext.SuppressionSettings.ToList();

                if (foundSettings != null && foundSettings.Count > 0)
                {
                    pam2EntitiesContext.SuppressionSettings.RemoveRange(foundSettings);

                }
                SuppressionSetting newSetting = new SuppressionSetting();
                newSetting.Value = value;
                newSetting.CreatedBy = new Guid(PAMUserId);
                newSetting.CreatedDate = DateTime.UtcNow;
                pam2EntitiesContext.SuppressionSettings.Add(newSetting);
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetSuppressionSetting()
        {
            string Value = string.Empty;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                SuppressionSetting foundSetting = null;
                foundSetting = pam2EntitiesContext.SuppressionSettings.FirstOrDefault();
                if (foundSetting != null)
                {
                    Value = foundSetting.Value;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Value;
        }

        #endregion

        #region Auto Promote

        public void UpdateIsPrimary(Guid sessionResultId, bool isPrimary)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                SessionResult foundSessionResult = pam2EntitiesContext.SessionResults.Where(s => s.SessionResultId == sessionResultId).FirstOrDefault();

                if (foundSessionResult != null)
                {
                    foundSessionResult.IsPrimary = isPrimary;
                    pam2EntitiesContext.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Memo Field Configuration

        public void AddMemoFieldConfiguration(MemoFieldConfiguration configuration)
        {
            MemoFieldConfiguration foundConfiguration = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundConfiguration = pam2EntitiesContext.MemoFieldConfigurations.Where(c => c.SchemaName == configuration.SchemaName).FirstOrDefault();
                if (foundConfiguration != null)
                {
                    foundConfiguration.CustomName = configuration.CustomName;
                    foundConfiguration.DisplayName = configuration.DisplayName;
                    foundConfiguration.DefaultAction = configuration.DefaultAction;
                    foundConfiguration.EntitySettingId = configuration.EntitySettingId;
                    foundConfiguration.ModifiedBy = configuration.ModifiedBy;
                    foundConfiguration.ModifiedDate = DateTime.UtcNow;
                    foundConfiguration.SchemaName = configuration.SchemaName;
                    pam2EntitiesContext.SaveChanges();
                }
                else
                {
                    foundConfiguration = configuration;
                    foundConfiguration.CreatedDate = DateTime.UtcNow;
                    foundConfiguration.ModifiedDate = null;
                    foundConfiguration.ModifiedBy = null;
                    pam2EntitiesContext.MemoFieldConfigurations.Add(foundConfiguration);
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void DeleteMemoFieldConfiguration(MemoFieldConfiguration configuration)
        {
            MemoFieldConfiguration foundConfiguration = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundConfiguration = pam2EntitiesContext.MemoFieldConfigurations.Where(c => c.SchemaName == configuration.SchemaName).FirstOrDefault();
                if (foundConfiguration != null)
                {
                    pam2EntitiesContext.MemoFieldConfigurations.Remove(foundConfiguration);
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public List<MemoFieldConfiguration> GetMemoFieldConfiguration(string entitySettingId)
        {
            List<MemoFieldConfiguration> foundConfigurations = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundConfigurations = pam2EntitiesContext.MemoFieldConfigurations.Where(c => c.EntitySettingId == new Guid(entitySettingId)).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return foundConfigurations;
        }


        #endregion

        #region AutoMerge

        public List<PAM1Attribute> GetSectionsAttributeSettingsForAutoMerge(string entitySettingId)
        {
            List<PAM1Attribute> lstPam1Attributes = new List<PAM1Attribute>();
            Guid entitySettingIdGUID = new Guid(entitySettingId);

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                //List<Section> lstSections = (from s in pam2EntitiesContext.Sections
                // join ss in pam2EntitiesContext.SessionSections on s.SectionId equals ss.SectionId
                //into resultSec
                // from newSec in resultSec.DefaultIfEmpty()
                // where newSec == null
                //  select s).ToList<Section>();
                var lstResult = (from s in pam2EntitiesContext.Sections
                                 join a in pam2EntitiesContext.AttributeSettings.OrderBy(c => c.DisplayOrder) on s.SectionId equals a.SectionId
                                 into result
                                 from AttrSetting in result.DefaultIfEmpty()
                                 let index = AttrSetting == null ? 0 : AttrSetting.DisplayOrder
                                 orderby s.DisplayOrder, index
                                 where s.EntitySettingId == entitySettingIdGUID && AttrSetting.UseForAutoMerge == true && (AttrSetting == null || (AttrSetting.SessionId == null && AttrSetting.CustomName.ToLower() != "header" && AttrSetting.SectionId != null))
                                 select new { Section = s, AttrSetting }
                                 ).Distinct().ToList();


                string sectionName = "";
                int sectionOrder = 0;
                List<Section> AllSections = (from sectionSetting in pam2EntitiesContext.Sections
                                             select sectionSetting).ToList();
                foreach (var obj in lstResult)
                {
                    if (obj == null)
                        continue;
                    if (obj.AttrSetting != null && obj.AttrSetting.AttributeSettingId != null && obj.AttrSetting.AttributeSettingId != Guid.Empty && obj.AttrSetting.CustomName.ToLower() != "header")
                    {
                        sectionName = "Default";
                        sectionOrder = 0;
                        PAM1Attribute attribute = new PAM1Attribute();
                        attribute.Id = obj.AttrSetting.AttributeSettingId.ToString();
                        attribute.FieldName = obj.AttrSetting.SchemaName.ToLower();
                        attribute.Value = "!#!Value!#!";
                        attribute.Type = "!#!Type!#!";
                        //  attribute.Entity = entityName;
                        if (AllSections != null && AllSections.Count > 0 && obj.Section.SectionId != null && obj.Section.SectionId != Guid.Empty)
                        {
                            var section = (from sectionSetting in AllSections
                                           where sectionSetting.SectionId == obj.Section.SectionId
                                           select sectionSetting).FirstOrDefault();

                            if (section != null)
                            {
                                sectionOrder = section.DisplayOrder;
                                sectionName = section.SectionName;
                            }
                        }
                        attribute.SectionDisplayOrder = sectionOrder;
                        attribute.GroupName = sectionName;
                        attribute.CustomName = obj.AttrSetting.CustomName;
                        attribute.ActualValue = "";
                        attribute.DisplayOrder = Convert.ToInt32(obj.AttrSetting.DisplayOrder);
                        attribute.DisplayName = obj.AttrSetting.DisplayName;
                        attribute.IsHidden = !(obj.AttrSetting.IsVisible == null ? true : Convert.ToBoolean(obj.AttrSetting.IsVisible));
                        attribute.HandleManually = (obj.AttrSetting.ExcludeUpdate == null ? false : Convert.ToBoolean(obj.AttrSetting.ExcludeUpdate));
                        lstPam1Attributes.Add(attribute);
                    }
                }

            }
            catch (Exception excObj)
            {
                throw excObj;
            }
            return lstPam1Attributes;
        }

        #endregion

        #region New Supppression Logic Change

        public string GetSessionEntityName(string sessionId)
        {
            var entityName = string.Empty;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                var foundSessionObj = pam2EntitiesContext.Sessions.Where(s => s.SessionId == new Guid(sessionId)).FirstOrDefault();
                if (foundSessionObj != null)
                {
                    var entitySetting = pam2EntitiesContext.EntitySettings.Where(es => es.EntitySettingId == foundSessionObj.EntitySettingId).FirstOrDefault();
                    if (entitySetting != null)
                    {
                        entityName = entitySetting.EntityLogicalName;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return entityName;
        }

        public List<SessionResult> GetAllSessionResults(string sessionId)
        {
            List<SessionResult> lstSessionResults = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstSessionResults = pam2EntitiesContext.SessionResults.Where(x => x.SessionId == new Guid(sessionId)).ToList().OrderBy(x => x.GroupNo).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;
        }


        public List<SessionResult> GetSessionResultsBetweenGroupRange(string sessionId, decimal minGroupNo, decimal maxGroupNo)
        {
            List<SessionResult> lstSessionResults = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstSessionResults = pam2EntitiesContext.SessionResults.Where(x =>
                    x.SessionId == new Guid(sessionId)
                    && x.GroupNo >= minGroupNo
                    && x.GroupNo <= maxGroupNo).ToList().OrderBy(x => x.GroupNo).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;
        }

        public List<SessionResult> GetSessionResultsForAutoFill(List<string> SessionResultIDs)
        {
            List<SessionResult> lstSessionResults = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstSessionResults = pam2EntitiesContext.SessionResults.Where(x => SessionResultIDs.Contains(x.SessionResultId.ToString())).ToList<SessionResult>();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;
        }

        //public List<SessionResult> GetSessionResultsBetweenGroupRange(string sessionId, decimal minGroupNo, decimal maxGroupNo)
        //{
        //    List<SessionResult> lstSessionResults = null;
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        lstSessionResults = pam2EntitiesContext.SessionResults.Where(x =>
        //            x.SessionId == new Guid(sessionId)
        //            && x.GroupNo >= minGroupNo
        //            && x.GroupNo <= maxGroupNo).ToList().OrderBy(x => x.GroupNo).ToList();

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    return lstSessionResults;
        //}

        public List<SessionResult> GetAllValidGroupSessionResults(string sessionId, decimal groupNo, List<Guid?> lstStatus = null)
        {
            List<SessionResult> lstSessionResults = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstSessionResults = (from u in pam2EntitiesContext.SessionResults
                                     where u.SessionId == new Guid(sessionId) && u.GroupNo == groupNo && (u.ValidGroup == true)
                                     && (lstSessionResults != null ? lstStatus.Contains(u.ReviewStatus) : true)
                                     orderby u.IsPrimary descending, u.GroupRank descending, u.GroupNo ascending
                                     select u).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;
        }

        #endregion

        #region Sub Group

        public decimal GetNewGroupNo(decimal groupNo, Guid sessionId)
        {
            decimal newGroupNo = groupNo;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                decimal wholeNumber = Math.Truncate(groupNo);
                decimal nextWholeNo = wholeNumber + 1;
                var groupNos = pam2EntitiesContext.SessionResults.Where(x => x.GroupNo > groupNo && x.GroupNo < nextWholeNo && x.SessionId == sessionId).OrderByDescending(x => x.GroupNo).ToList();

                if (groupNos != null && groupNos.Count() > 0)
                {
                    newGroupNo = groupNos[0].GroupNo + new decimal(0.01);
                }
                else
                {
                    newGroupNo = groupNo + new decimal(0.01);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return newGroupNo;
        }

        public void ReleaseSubGroup(decimal newGroupNo, List<string> SessionResultIds, string sessionId, decimal groupNo, string userId,
            string reviewingStatusEnum, string declinedStatusEnum, string pSuppressedStatusEnum,
            string unprocessedStatusEnum, string[] AutoStatusEnums,
            out Dictionary<decimal, List<SessionResult>> dicGroupWiseSessionResults, string userNotes)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);
                dicGroupWiseSessionResults = new Dictionary<decimal, List<SessionResult>>();

                string s1 = String.Empty, s2 = String.Empty, s3 = String.Empty;
                if(AutoStatusEnums.Length==3)
                {
                     s1 = AutoStatusEnums[0];
                     s2 = AutoStatusEnums[1];
                     s3 = AutoStatusEnums[2];
                }

                List<Status> lstStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum || st.Enum == declinedStatusEnum || 
                    st.Enum == pSuppressedStatusEnum || st.Enum == unprocessedStatusEnum || st.Enum == s1 || st.Enum == s2 || st.Enum == s3).ToList();
                if (lstStatus != null && lstStatus.Count > 0)
                {
                    lstStatus.ForEach(st => lstReviewStatus.Add(st.StatusId));
                }
                var query = pam2EntitiesContext.SessionResults.Where(s => ((s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.Reviewer == new Guid(userId)) && (lstReviewStatus.Contains(s.ReviewStatus)) && SessionResultIds.Contains(s.SessionResultId.ToString())));

                if (query != null && query.Count() > 0)
                {
                    SessionResult item = query.OrderByDescending(x => x.MatchScore).Take(1).FirstOrDefault();
                    SessionResult primaryItem = query.Where(x => x.IsPrimary == true).FirstOrDefault();

                    if (item != null)
                    {
                        Status UnProcessedStatus = lstStatus.Where(x => x.Enum == unprocessedStatusEnum).FirstOrDefault();
                        if (UnProcessedStatus != null)
                        {

                            dicGroupWiseSessionResults.Add(newGroupNo, query.ToList());
                            query.ToList().ForEach(
                                x =>
                                {
                                    x.ReviewStatus = UnProcessedStatus.StatusId;
                                    x.Reviewer = null;
                                    x.PunchIn = null;
                                    x.CurentStatusDateTime = DateTime.UtcNow;
                                    x.GroupNo = newGroupNo;
                                    x.IsPrimary = false;
                                    if (item.SessionResultId == x.SessionResultId)
                                    {
                                        x.IsPrimary = true;
                                        x.UserNotes = userNotes;
                                    }
                                });
                            pam2EntitiesContext.SaveChanges();
                        }
                    }

                    var query2 = pam2EntitiesContext.SessionResults.Where(s => ((s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.Reviewer == new Guid(userId)) && (lstReviewStatus.Contains(s.ReviewStatus)) && !SessionResultIds.Contains(s.SessionResultId.ToString())));
                    if (query2 != null && query2.Count() > 0)
                    {
                        dicGroupWiseSessionResults.Add(groupNo, query2.ToList());
                        if (primaryItem != null)
                        {
                            SessionResult item2 = query2.OrderByDescending(x => x.MatchScore).Take(1).FirstOrDefault();
                            if (item2 != null)
                            {
                                // ((SessionResult)item2).IsPrimary = true;
                                query2.ToList().ForEach(
                                x =>
                                {
                                    x.IsPrimary = false;
                                    if (item2.SessionResultId == x.SessionResultId)
                                    {
                                        x.IsPrimary = true;
                                    }
                                });
                                pam2EntitiesContext.SaveChanges();
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void AssignSubGroup(decimal newGroupNo, List<string> SessionResultIds, string sessionId, decimal groupNo, string userId,
            string selectedUserId, string reviewingStatusEnum, string declinedStatusEnum, string pSuppressedStatusEnum,
            string unprocessedStatusEnum, string[] AutoStatusEnums,
            out Dictionary<decimal, List<SessionResult>> dicGroupWiseSessionResults, string userNotes)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Guid?> lstReviewStatus = new List<Guid?>();
                lstReviewStatus.Add(null);
                dicGroupWiseSessionResults = new Dictionary<decimal, List<SessionResult>>();

                string s1 = String.Empty, s2 = String.Empty, s3 = String.Empty;
                if (AutoStatusEnums.Length == 3)
                {
                    s1 = AutoStatusEnums[0];
                    s2 = AutoStatusEnums[1];
                    s3 = AutoStatusEnums[2];
                }

                List<Status> lstStatus = pam2EntitiesContext.Status.Where(st => st.Enum == reviewingStatusEnum || st.Enum == declinedStatusEnum ||
                    st.Enum == pSuppressedStatusEnum || st.Enum == unprocessedStatusEnum || st.Enum == s1 || st.Enum == s2 || st.Enum == s3).ToList();
                if (lstStatus != null && lstStatus.Count > 0)
                {
                    lstStatus.ForEach(st => lstReviewStatus.Add(st.StatusId));
                    var query = pam2EntitiesContext.SessionResults.Where(s => ((s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.Reviewer == new Guid(userId)) && (lstReviewStatus.Contains(s.ReviewStatus)) && SessionResultIds.Contains(s.SessionResultId.ToString())));
                    if (query != null && query.Count() > 0)
                    {
                        SessionResult item = query.OrderByDescending(x => x.MatchScore).Take(1).FirstOrDefault();
                        SessionResult primaryItem = query.Where(x => x.IsPrimary == true).FirstOrDefault();

                        if (item != null)
                        {
                            dicGroupWiseSessionResults.Add(newGroupNo, query.ToList());

                            query.ToList().ForEach(x =>
                            {
                                x.ReviewStatus = (x.Status1.Enum == s1 || x.Status1.Enum == s2 || x.Status1.Enum == s3) ? x.ReviewStatus : lstStatus.Where(st => st.Enum == reviewingStatusEnum).FirstOrDefault().StatusId;
                                x.Reviewer = new Guid(selectedUserId);
                                x.PunchIn = DateTime.UtcNow;
                                x.IsPrimary = false;
                                x.GroupNo = newGroupNo;
                                x.CurentStatusDateTime = DateTime.UtcNow;
                                if (((SessionResult)item).SessionResultId == x.SessionResultId)
                                {
                                    x.IsPrimary = true;
                                    x.UserNotes = userNotes;
                                }
                            });
                            pam2EntitiesContext.SaveChanges();
                        }

                        var query2 = pam2EntitiesContext.SessionResults.Where(s => ((s.SessionId == new Guid(sessionId) && s.GroupNo == groupNo && s.Reviewer == new Guid(userId)) && (lstReviewStatus.Contains(s.ReviewStatus)) && !SessionResultIds.Contains(s.SessionResultId.ToString())));
                        if (query2 != null && query2.Count() > 0)
                        {
                            dicGroupWiseSessionResults.Add(groupNo, query2.ToList());
                            if (primaryItem != null)
                            {
                                SessionResult item2 = query2.OrderByDescending(x => x.MatchScore).Take(1).FirstOrDefault();
                                if (item2 != null)
                                {
                                    // ((SessionResult)item2).IsPrimary = true;
                                    query2.ToList().ForEach(
                                    x =>
                                    {
                                        x.IsPrimary = false;
                                        if (item2.SessionResultId == x.SessionResultId)
                                        {
                                            x.IsPrimary = true;
                                        }
                                    });
                                    pam2EntitiesContext.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #endregion

        #region Inactive Record Settings

        public void SaveInactiveRecordSettings(string value, string PAMUserId)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<InactiveRecordSetting> foundSettings = null;
                foundSettings = pam2EntitiesContext.InactiveRecordSettings.ToList();

                if (foundSettings != null && foundSettings.Count > 0)
                {
                    pam2EntitiesContext.InactiveRecordSettings.RemoveRange(foundSettings);

                }
                InactiveRecordSetting newSetting = new InactiveRecordSetting();
                newSetting.Value = value;
                newSetting.CreatedBy = new Guid(PAMUserId);
                newSetting.CreatedDate = DateTime.UtcNow;
                pam2EntitiesContext.InactiveRecordSettings.Add(newSetting);
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string GetInactiveRecordSetting()
        {
            string Value = string.Empty;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                InactiveRecordSetting foundSetting = null;
                foundSetting = pam2EntitiesContext.InactiveRecordSettings.FirstOrDefault();
                if (foundSetting != null)
                {
                    Value = foundSetting.Value;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Value;
        }

        #endregion

        #region Customer License

        public List<SessionResult> GetAllValidReviewedPrimarySessionResults(string sessionId, string pSuppressedStatusEnum, string unprocessedStatusEnum)
        {
            List<SessionResult> lstSessionResult = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Status> allStatus = GetAllStatus();
                Status pSuppressedStatus = null;
                Status unprocessedStatus = null;
                if (allStatus != null && allStatus.Count > 0)
                {
                    pSuppressedStatus = allStatus.Where(st => st.Enum == pSuppressedStatusEnum).FirstOrDefault();
                    unprocessedStatus = allStatus.Where(st => st.Enum == unprocessedStatusEnum).FirstOrDefault();
                    lstSessionResult = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId)
                        && s.ValidGroup == true && s.IsPrimary == true
                        && (s.ReviewStatus != pSuppressedStatus.StatusId
                        && s.ReviewStatus != unprocessedStatus.StatusId
                        && s.ReviewStatus != null
                        )).ToList<SessionResult>();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResult;
        }

        public List<SessionResult> GetAllValidReviewedPrimarySessionResultsDoNotEndsWithZero(string sessionId, string pSuppressedStatusEnum, string unprocessedStatusEnum)
        {
            List<SessionResult> lstSessionResult = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString + ";MultipleActiveResultSets=True;");
                List<Status> allStatus = GetAllStatus();
                Status pSuppressedStatus = null;
                Status unprocessedStatus = null;
                if (allStatus != null && allStatus.Count > 0)
                {
                    pSuppressedStatus = allStatus.Where(st => st.Enum == pSuppressedStatusEnum).FirstOrDefault();
                    unprocessedStatus = allStatus.Where(st => st.Enum == unprocessedStatusEnum).FirstOrDefault();
                    lstSessionResult = pam2EntitiesContext.SessionResults.Where(s => s.SessionId == new Guid(sessionId)
                        && s.ValidGroup == true && s.IsPrimary == true
                        && (s.ReviewStatus != pSuppressedStatus.StatusId
                        && s.ReviewStatus != unprocessedStatus.StatusId
                        && s.ReviewStatus != null && !((s.GroupNo % 1) == 0))).ToList<SessionResult>();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResult;
        }



        #endregion

        #region NoOfSessionsPerEntity

        public bool AddNoOfSessionsPerEntityRecord(NoOfSessionsPerEntityUsage record, Guid pamUserId)
        {
            bool result = false;
            NoOfSessionsPerEntityUsage foundRecord = null;
            try
            {
                InsertNoOfGroupsPerSessionUsageHistory("Before Add", pamUserId.ToString());

                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecord = pam2EntitiesContext.NoOfSessionsPerEntityUsages.Where(rec => rec.EntitySettingId == record.EntitySettingId && rec.PackageId == record.PackageId).FirstOrDefault();
                if (foundRecord != null)
                {
                    foundRecord.UsedValue += 1;
                    pam2EntitiesContext.SaveChanges();
                }
                else
                {
                    foundRecord = new NoOfSessionsPerEntityUsage();
                    foundRecord.EntitySettingId = record.EntitySettingId;
                    foundRecord.PackageId = record.PackageId;
                    foundRecord.UsedValue = 1;
                    pam2EntitiesContext.NoOfSessionsPerEntityUsages.Add(foundRecord);
                    pam2EntitiesContext.SaveChanges();
                }

                InsertNoOfGroupsPerSessionUsageHistory("After Add", pamUserId.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public List<NoOfSessionsPerEntityUsage> GetNoOfSessionsPerEntityRecords(Guid entitySettingId, Guid packageId)
        {
            List<NoOfSessionsPerEntityUsage> foundRecords = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecords = pam2EntitiesContext.NoOfSessionsPerEntityUsages.Where(rec => rec.EntitySettingId == entitySettingId && rec.PackageId == packageId).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return foundRecords;
        }

        public bool UpdateNoOfSessionsPerEntityUsedValue(Guid entitySettingId, Guid packageId, int usedValue, Guid pamUserId)
        {
            bool result = false;
            NoOfSessionsPerEntityUsage foundRecord = null;
            try
            {
                InsertNoOfGroupsPerSessionUsageHistory("Before Update", pamUserId.ToString());
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecord = pam2EntitiesContext.NoOfSessionsPerEntityUsages.Where(rec => rec.EntitySettingId == entitySettingId && rec.PackageId == packageId).FirstOrDefault();
                if (foundRecord != null)
                {
                    foundRecord.UsedValue = usedValue;
                    pam2EntitiesContext.SaveChanges();
                }
                else
                {
                    foundRecord = new NoOfSessionsPerEntityUsage();
                    foundRecord.EntitySettingId = entitySettingId;
                    foundRecord.PackageId = packageId;
                    foundRecord.UsedValue = usedValue;
                    pam2EntitiesContext.NoOfSessionsPerEntityUsages.Add(foundRecord);
                    pam2EntitiesContext.SaveChanges();
                }
                InsertNoOfGroupsPerSessionUsageHistory("After Update", pamUserId.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }


        #endregion

        #region  NoOfGroupsPerSession

        public bool AddNoOfGroupsPerSessionRecord(NoOfGroupsPerSessionUsage record, Guid pamUserId)
        {
            bool result = false;
            NoOfGroupsPerSessionUsage foundRecord = null;
            try
            {
                InsertNoOfSessionsPerEntityUsageHistory("Before Add", pamUserId.ToString());
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecord = pam2EntitiesContext.NoOfGroupsPerSessionUsages.Where(rec => rec.SessionId == record.SessionId && rec.PackageId == record.PackageId).FirstOrDefault();
                if (foundRecord != null)
                {
                    foundRecord.UsedValue += 1;
                    pam2EntitiesContext.SaveChanges();
                }
                else
                {
                    foundRecord = new NoOfGroupsPerSessionUsage();
                    foundRecord.SessionId = record.SessionId;
                    foundRecord.PackageId = record.PackageId;
                    foundRecord.UsedValue = 1;
                    pam2EntitiesContext.NoOfGroupsPerSessionUsages.Add(foundRecord);
                    pam2EntitiesContext.SaveChanges();
                }
                InsertNoOfSessionsPerEntityUsageHistory("After Add", pamUserId.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public bool DecreaseNoOfGroupsPerSessionUsedValue(Guid pamUserId, Guid sessionId, Guid packageId)
        {
            bool result = false;
            NoOfGroupsPerSessionUsage foundRecord = null;
            try
            {
                InsertNoOfSessionsPerEntityUsageHistory("Before decrease", pamUserId.ToString());
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecord = pam2EntitiesContext.NoOfGroupsPerSessionUsages.Where(rec => rec.SessionId == sessionId && rec.PackageId == packageId).FirstOrDefault();
                if (foundRecord != null)
                {
                    foundRecord.UsedValue -= 1;
                    pam2EntitiesContext.SaveChanges();
                }

                InsertNoOfSessionsPerEntityUsageHistory("After decrease", pamUserId.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }


        public bool UpdateNoOfGroupsPerSessionUsedValue(Guid sessionId, Guid packageId, int usedValue, Guid pamUserId)
        {
            bool result = false;
            NoOfGroupsPerSessionUsage foundRecord = null;
            try
            {
                InsertNoOfSessionsPerEntityUsageHistory("Before Update", pamUserId.ToString());
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecord = pam2EntitiesContext.NoOfGroupsPerSessionUsages.Where(rec => rec.SessionId == sessionId && rec.PackageId == packageId).FirstOrDefault();
                if (foundRecord != null)
                {
                    foundRecord.UsedValue = usedValue;
                    pam2EntitiesContext.SaveChanges();
                }
                else
                {
                    foundRecord = new NoOfGroupsPerSessionUsage();
                    foundRecord.SessionId = sessionId;
                    foundRecord.PackageId = packageId;
                    foundRecord.UsedValue = usedValue;
                    pam2EntitiesContext.NoOfGroupsPerSessionUsages.Add(foundRecord);
                    pam2EntitiesContext.SaveChanges();
                }
                InsertNoOfSessionsPerEntityUsageHistory("After Update", pamUserId.ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }


        public List<NoOfGroupsPerSessionUsage> GetNoOfGroupsPerSessionRecords(Guid sessionId, Guid packageId)
        {
            List<NoOfGroupsPerSessionUsage> foundRecords = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                foundRecords = pam2EntitiesContext.NoOfGroupsPerSessionUsages.Where(rec => rec.SessionId == sessionId && rec.PackageId == packageId).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return foundRecords;
        }


        public bool InsertNoOfGroupsPerSessionUsageHistory(string operationType, string UpdatedBy = null)
        {
            bool result = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnString))
                {
                    if (conn != null && conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        // This will be when the change is occuring from P&M application
                        if (!string.IsNullOrWhiteSpace(UpdatedBy))
                        {
                            cmd.CommandText = @"INSERT INTO [NoOfGroupsPerSessionUsageHistory] ([SessionId],[PackageId],[UsedValue],[UpdateDate],[UpdatedBy],OperationType ) 
                                            SELECT [SessionId],[PackageId],[UsedValue],GETUTCDATE(),@UpdatedBy,@OperationType FROM NoOfGroupsPerSessionUsage";
                            cmd.Parameters.AddWithValue("@UpdatedBy", UpdatedBy);
                            cmd.Parameters.AddWithValue("@OperationType", operationType);
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            // This will be when operation is done through Super Admin Screen
                            cmd.CommandText = @"INSERT INTO [NoOfGroupsPerSessionUsageHistory] ([SessionId],[PackageId],[UsedValue],[UpdateDate],OperationType ) 
                                            SELECT [SessionId],[PackageId],[UsedValue],GETUTCDATE(),@OperationType FROM NoOfGroupsPerSessionUsage";
                            cmd.Parameters.AddWithValue("@OperationType", operationType);
                            cmd.ExecuteNonQuery();
                        }

                        result = true;
                        if (conn.State != ConnectionState.Closed)
                        {
                            conn.Close();
                        }
                    }

                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }

            return result;
        }

        public bool InsertNoOfSessionsPerEntityUsageHistory(string operationType, string UpdatedBy = null)
        {
            bool result = false;
            try
            {
                using (SqlConnection conn = new SqlConnection(sqlConnString))
                {
                    if (conn != null && conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        if (!string.IsNullOrWhiteSpace(UpdatedBy))
                        {
                            cmd.CommandText = @"INSERT INTO [NoOfSessionsPerEntityUsageHistory] ([PackageId],[EntitySettingId],[UsedValue],[UpdateDate],[UpdatedBy]
                            ,[OperationType]) SELECT [PackageId],[EntitySettingId],[UsedValue],GETUTCDATE(),@UpdatedBy,@OperationType FROM NoOfSessionsPerEntityUsage";
                            cmd.Parameters.AddWithValue("@UpdatedBy", UpdatedBy);
                            cmd.Parameters.AddWithValue("@OperationType", operationType);
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            cmd.CommandText = @"INSERT INTO [NoOfSessionsPerEntityUsageHistory] ([PackageId],[EntitySettingId],[UsedValue],[UpdateDate],[OperationType]) 
                            SELECT [PackageId],[EntitySettingId],[UsedValue],GETUTCDATE(),@OperationType FROM NoOfSessionsPerEntityUsage";
                            cmd.Parameters.AddWithValue("@OperationType", operationType);
                            cmd.ExecuteNonQuery();
                        }

                        result = true;
                        if (conn.State != ConnectionState.Closed)
                        {
                            conn.Close();
                        }
                    }

                }
            }
            catch (Exception excObj)
            {
                throw excObj;
            }

            return result;
        }



        #endregion

        #region Status Reason

        public void SaveStatusReason(string StatusReason, string StatusReasonText, string Entity, string UserId)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid g = new Guid(Entity);
                StatusReasonSetting objStatusReasonSetting = (from c in pam2EntitiesContext.StatusReasonSettings
                                                              where c.EntitySettingId == g
                                                              select c).FirstOrDefault();
                if (objStatusReasonSetting == null)
                {
                    objStatusReasonSetting = new StatusReasonSetting();
                    objStatusReasonSetting.StatusReasonSettingsId = Guid.NewGuid();
                    objStatusReasonSetting.EntitySettingId = new Guid(Entity);
                    objStatusReasonSetting.StatusReasonFieldSchema = StatusReason;
                    objStatusReasonSetting.StatusReasonFieldDisplay = StatusReasonText;
                    objStatusReasonSetting.CreatedBy = new Guid(UserId);
                    objStatusReasonSetting.CreatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.StatusReasonSettings.Add(objStatusReasonSetting);
                    pam2EntitiesContext.SaveChanges();
                }
                else
                {
                    objStatusReasonSetting.StatusReasonFieldSchema = StatusReason;
                    objStatusReasonSetting.StatusReasonFieldDisplay = StatusReasonText;
                    objStatusReasonSetting.UpdatedBy = new Guid(UserId);
                    objStatusReasonSetting.UpdatedDate = DateTime.UtcNow;
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public PAMStatusReasonSettingsResult GetAllStatusReasons()
        {
            PAMStatusReasonSettingsResult objPAMStatusReasonSettingsResult = new PAMStatusReasonSettingsResult();
            List<PAMStatusReasonSettings> lstPAMStatusReasonSettings = new List<PAMStatusReasonSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<StatusReasonSetting> lstStatusReasonSetting = (from c in pam2EntitiesContext.StatusReasonSettings
                                                                    select c).ToList<StatusReasonSetting>();
                if (lstStatusReasonSetting != null)
                {
                    EntitySettingResultSet entitySettings = GetEntitySettings();

                    foreach (StatusReasonSetting objStatusReasonSetting in lstStatusReasonSetting)
                    {
                        PAMStatusReasonSettings objPAMStatusReasonSettings = new PAMStatusReasonSettings();
                        objPAMStatusReasonSettings.StatusReasonSettingsId = objStatusReasonSetting.StatusReasonSettingsId;
                        objPAMStatusReasonSettings.StatusReasonFieldSchema = objStatusReasonSetting.StatusReasonFieldSchema;
                        objPAMStatusReasonSettings.StatusReasonFieldDisplay = objStatusReasonSetting.StatusReasonFieldDisplay;
                        objPAMStatusReasonSettings.EntitySettingId = new Guid(Convert.ToString(objStatusReasonSetting.EntitySettingId));

                        if (entitySettings != null && entitySettings.EntitySettings != null && entitySettings.EntitySettings.Count > 0)
                        {
                            PAM2EntitySetting entitySetting = (from entitySetting1 in entitySettings.EntitySettings
                                                               where entitySetting1.EntitySettingId.ToLower() == Convert.ToString(objStatusReasonSetting.EntitySettingId).ToLower()
                                                               select entitySetting1).FirstOrDefault();
                            if (entitySetting != null)
                            {
                                objPAMStatusReasonSettings.EntityName = entitySetting.EntityDisplayName;
                            }
                        }

                        lstPAMStatusReasonSettings.Add(objPAMStatusReasonSettings);

                    }

                    objPAMStatusReasonSettingsResult.StatusReasonSettings = lstPAMStatusReasonSettings;
                    objPAMStatusReasonSettingsResult.Result = true;
                    objPAMStatusReasonSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMStatusReasonSettingsResult;
        }

        #endregion

        #region Best Record Detection

        public List<SessionResult> GetAllValidGroupNumbersFromSessionResults(string sessionId)
        {
            List<SessionResult> lstSessionResults = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                lstSessionResults = pam2EntitiesContext.SessionResults.Where(x => x.SessionId == new Guid(sessionId) && x.ValidGroup == true).ToList().OrderBy(x => x.GroupNo).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lstSessionResults;
        }

        public PAMBestRecordDetectionSettingsResult GetBestRecordDetectionRules()
        {
            PAMBestRecordDetectionSettingsResult objPAMBestRecordDetectionSettingsResult = new PAMBestRecordDetectionSettingsResult();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettings = new List<PAMBestRecordDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<BestRecordDetectionRule> lstBestRecordDetectionRule = (from c in pam2EntitiesContext.BestRecordDetectionRules
                                                                        select c).ToList<BestRecordDetectionRule>();
                if (lstBestRecordDetectionRule != null)
                {
                    foreach (BestRecordDetectionRule objBestRecordDetectionRule in lstBestRecordDetectionRule)
                    {
                        PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettings = new PAMBestRecordDetectionSettings();
                        objPAMBestRecordDetectionSettings.Id = objBestRecordDetectionRule.Id;
                      //  objPAMBestRecordDetectionSettings.RuleId = objBestRecordDetectionRule.Id;
                        objPAMBestRecordDetectionSettings.RuleEnum = objBestRecordDetectionRule.RuleEnum;
                        objPAMBestRecordDetectionSettings.RuleName = objBestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.Account = Convert.ToBoolean(objBestRecordDetectionRule.Account);
                        objPAMBestRecordDetectionSettings.Contact = Convert.ToBoolean(objBestRecordDetectionRule.Contact);
                        objPAMBestRecordDetectionSettings.Lead = Convert.ToBoolean(objBestRecordDetectionRule.Lead);

                         objPAMBestRecordDetectionSettings.leaf = true;
                         objPAMBestRecordDetectionSettings.cls = "file";
                         objPAMBestRecordDetectionSettings.text = objBestRecordDetectionRule.RuleName;
                         objPAMBestRecordDetectionSettings.qtip = objBestRecordDetectionRule.RuleName;
                         objPAMBestRecordDetectionSettings.id = objBestRecordDetectionRule.Id.ToString();
                         lstPAMBestRecordDetectionSettings.Add(objPAMBestRecordDetectionSettings);
                    }


                    objPAMBestRecordDetectionSettingsResult.BestRecordDetectionSettings = lstPAMBestRecordDetectionSettings;
                    objPAMBestRecordDetectionSettingsResult.Result = true;
                    objPAMBestRecordDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestRecordDetectionSettingsResult;
        }

        public PAMBestRecordDetectionSettingsResult GetBestRecordDetectionRulesEntitywise(string EntitySettingsId)
        {
            PAMBestRecordDetectionSettingsResult objPAMBestRecordDetectionSettingsResult = new PAMBestRecordDetectionSettingsResult();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettings = new List<PAMBestRecordDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<BestRecordDetectionRule> lstBestRecordDetectionRule = new List<BestRecordDetectionRule>();
                EntitySetting objEntitySetting = GetEntitySettingbyID(EntitySettingsId);

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account")
                {
                    lstBestRecordDetectionRule = (from c in pam2EntitiesContext.BestRecordDetectionRules
                                                  where c.Account == true
                                                  select c).ToList<BestRecordDetectionRule>();
                }

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "contact")
                {
                    lstBestRecordDetectionRule = (from c in pam2EntitiesContext.BestRecordDetectionRules
                                                  where c.Contact == true
                                                  select c).ToList<BestRecordDetectionRule>();
                }

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "lead")
                {
                    lstBestRecordDetectionRule = (from c in pam2EntitiesContext.BestRecordDetectionRules
                                                  where c.Lead == true
                                                  select c).ToList<BestRecordDetectionRule>();
                }

                if (lstBestRecordDetectionRule != null)
                {
                    foreach (BestRecordDetectionRule objBestRecordDetectionRule in lstBestRecordDetectionRule)
                    {
                        PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettings = new PAMBestRecordDetectionSettings();
                        objPAMBestRecordDetectionSettings.Id = objBestRecordDetectionRule.Id;
                        objPAMBestRecordDetectionSettings.RuleEnum = objBestRecordDetectionRule.RuleEnum;
                        objPAMBestRecordDetectionSettings.RuleName = objBestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.Account = Convert.ToBoolean(objBestRecordDetectionRule.Account);
                        objPAMBestRecordDetectionSettings.Contact = Convert.ToBoolean(objBestRecordDetectionRule.Contact);
                        objPAMBestRecordDetectionSettings.Lead = Convert.ToBoolean(objBestRecordDetectionRule.Lead);

                        objPAMBestRecordDetectionSettings.text = objBestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.qtip = objBestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.cls = "file";
                        objPAMBestRecordDetectionSettings.id = objBestRecordDetectionRule.Id.ToString();
                        objPAMBestRecordDetectionSettings.leaf = true;

                        lstPAMBestRecordDetectionSettings.Add(objPAMBestRecordDetectionSettings);
                    }

                    objPAMBestRecordDetectionSettingsResult.BestRecordDetectionSettings = lstPAMBestRecordDetectionSettings;
                    objPAMBestRecordDetectionSettingsResult.Result = true;
                    objPAMBestRecordDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestRecordDetectionSettingsResult;
        }

        public PAMBestRecordDetectionSettingsResult GetBestRecordDetectionSettingsEntitywise(string EntitySettingsId)
        {
            PAMBestRecordDetectionSettingsResult objPAMBestRecordDetectionSettingsResult = new PAMBestRecordDetectionSettingsResult();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettings = new List<PAMBestRecordDetectionSettings>();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettingsRoot = new List<PAMBestRecordDetectionSettings>();
            List<BestRecordDetectionSetting> lstBestRecordDetectionSetting = new List<BestRecordDetectionSetting>();

             try
             {
                 PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                 EntitySetting objEntitySetting = GetEntitySettingbyID(EntitySettingsId);

                 if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account")
                 {
                     lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                      join rules in pam2EntitiesContext.BestRecordDetectionRules
                                                      on c.RuleId equals rules.Id
                                                      where rules.Account == true && c.IsMaster== true
                                                      orderby c.OrderOfRules ascending
                                                      select c).ToList<BestRecordDetectionSetting>();
                 }

                 if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "contact")
                 {
                     lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                      join rules in pam2EntitiesContext.BestRecordDetectionRules
                                                      on c.RuleId equals rules.Id
                                                      where rules.Contact == true && c.IsMaster == true
                                                      orderby c.OrderOfRules ascending
                                                      select c).ToList<BestRecordDetectionSetting>();
                 }

                 if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "lead")
                 {
                     lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                      join rules in pam2EntitiesContext.BestRecordDetectionRules
                                                      on c.RuleId equals rules.Id
                                                      where rules.Lead == true && c.IsMaster == true
                                                      orderby c.OrderOfRules ascending
                                                      select c).ToList<BestRecordDetectionSetting>();
                 }

                 PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettingsRoot = new PAMBestRecordDetectionSettings();
                 objPAMBestRecordDetectionSettingsRoot.leaf = false;
                 objPAMBestRecordDetectionSettingsRoot.text = "Rules Sequence";
                 objPAMBestRecordDetectionSettingsRoot.qtip = "Rules Sequence";
                 objPAMBestRecordDetectionSettingsRoot.id = "1";
                 objPAMBestRecordDetectionSettingsRoot.cls = "folder";

                 if (lstBestRecordDetectionSetting != null)
                 {

                     foreach (BestRecordDetectionSetting objBestRecordDetectionSetting in lstBestRecordDetectionSetting)
                     {
                         PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettings = new PAMBestRecordDetectionSettings();

                         objPAMBestRecordDetectionSettings.Id = objBestRecordDetectionSetting.Id;
                         //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                         //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                         objPAMBestRecordDetectionSettings.RuleName = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                         objPAMBestRecordDetectionSettings.RuleParamId = objBestRecordDetectionSetting.RuleParamId;

                         if (objBestRecordDetectionSetting.BestRecordRuleParametersMaster != null)
                             objPAMBestRecordDetectionSettings.RuleParam = objBestRecordDetectionSetting.BestRecordRuleParametersMaster.Parameter;

                         objPAMBestRecordDetectionSettings.RuleId = new Guid(Convert.ToString(objBestRecordDetectionSetting.RuleId));
                         objPAMBestRecordDetectionSettings.RuleEnum = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleEnum;
                         objPAMBestRecordDetectionSettings.Account = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Account);
                         objPAMBestRecordDetectionSettings.Contact = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Contact);
                         objPAMBestRecordDetectionSettings.Lead = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Lead);
                         objPAMBestRecordDetectionSettings.leaf = true;
                         objPAMBestRecordDetectionSettings.cls = "file";
                         objPAMBestRecordDetectionSettings.text = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                         objPAMBestRecordDetectionSettings.qtip = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                         objPAMBestRecordDetectionSettings.id = objBestRecordDetectionSetting.Id.ToString();

                         lstPAMBestRecordDetectionSettings.Add(objPAMBestRecordDetectionSettings);
                     }

                     objPAMBestRecordDetectionSettingsRoot.children = lstPAMBestRecordDetectionSettings;

                     lstPAMBestRecordDetectionSettingsRoot.Add(objPAMBestRecordDetectionSettingsRoot);
                     objPAMBestRecordDetectionSettingsResult.BestRecordDetectionSettings = lstPAMBestRecordDetectionSettingsRoot;
                     objPAMBestRecordDetectionSettingsResult.Result = true;
                     objPAMBestRecordDetectionSettingsResult.success = true;
                 }
             }
             catch (Exception ex)
             {
                 throw ex;
             }

            return objPAMBestRecordDetectionSettingsResult;
        }

        public bool IsBestRecordDetectionSettingsForEntity(string EntitySettingsId)
        {

            bool IsRules = false;
            List<BestRecordDetectionSetting> lstBestRecordDetectionSetting = new List<BestRecordDetectionSetting>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                EntitySetting objEntitySetting = GetEntitySettingbyID(EntitySettingsId);

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account")
                {
                    lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                     join rules in pam2EntitiesContext.BestRecordDetectionRules
                                                     on c.RuleId equals rules.Id
                                                     where rules.Account == true && c.IsMaster == true
                                                     orderby c.OrderOfRules ascending
                                                     select c).ToList<BestRecordDetectionSetting>();
                }

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "contact")
                {
                    lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                     join rules in pam2EntitiesContext.BestRecordDetectionRules
                                                     on c.RuleId equals rules.Id
                                                     where rules.Contact == true && c.IsMaster == true
                                                     orderby c.OrderOfRules ascending
                                                     select c).ToList<BestRecordDetectionSetting>();
                }

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "lead")
                {
                    lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                     join rules in pam2EntitiesContext.BestRecordDetectionRules
                                                     on c.RuleId equals rules.Id
                                                     where rules.Lead == true && c.IsMaster == true
                                                     orderby c.OrderOfRules ascending
                                                     select c).ToList<BestRecordDetectionSetting>();
                }

                if (lstBestRecordDetectionSetting.Count > 0)
                {
                    IsRules = true;
                }

               
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return IsRules;
        }

        public PAMBestRecordDetectionSettingsResult GetBestRecordDetectionSettingsForSession(string SessionId)
        {
            PAMBestRecordDetectionSettingsResult objPAMBestRecordDetectionSettingsResult = new PAMBestRecordDetectionSettingsResult();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettings = new List<PAMBestRecordDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid gSessionId = new Guid(SessionId);

                List<BestRecordDetectionSetting> lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings join
                                                                                  sessionBestRecord in pam2EntitiesContext.SessionBestRecordSettings 
                                                                                  on c.Id equals sessionBestRecord.BestRecordDetectionSettingsId
                                                                                  where c.IsMaster == false && sessionBestRecord.SessionId == gSessionId
                                                                                  orderby c.OrderOfRules ascending
                                                                                  select c).ToList<BestRecordDetectionSetting>();
                if (lstBestRecordDetectionSetting != null)
                {
                    foreach (BestRecordDetectionSetting objBestRecordDetectionSetting in lstBestRecordDetectionSetting)
                    {
                        PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettings = new PAMBestRecordDetectionSettings();

                        objPAMBestRecordDetectionSettings.Id = objBestRecordDetectionSetting.Id;
                     //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                     //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestRecordDetectionSettings.RuleName = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.RuleParamId =  objBestRecordDetectionSetting.RuleParamId;

                        if (objBestRecordDetectionSetting.BestRecordRuleParametersMaster != null )
                            objPAMBestRecordDetectionSettings.RuleParam = objBestRecordDetectionSetting.BestRecordRuleParametersMaster.Parameter;

                        objPAMBestRecordDetectionSettings.RuleId = new Guid(Convert.ToString(objBestRecordDetectionSetting.RuleId));
                        objPAMBestRecordDetectionSettings.RuleEnum = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleEnum;
                        objPAMBestRecordDetectionSettings.Account = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Account);
                        objPAMBestRecordDetectionSettings.Contact = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Contact);
                        objPAMBestRecordDetectionSettings.Lead = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Lead);

                        lstPAMBestRecordDetectionSettings.Add(objPAMBestRecordDetectionSettings);
                    }

                    objPAMBestRecordDetectionSettingsResult.BestRecordDetectionSettings = lstPAMBestRecordDetectionSettings;
                    objPAMBestRecordDetectionSettingsResult.Result = true;
                    objPAMBestRecordDetectionSettingsResult.success = true;
                }  
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestRecordDetectionSettingsResult;
        }

        public PAMBestRecordDetectionSettingsResult GetBestRecordDetectionSettings()
        {
            PAMBestRecordDetectionSettingsResult objPAMBestRecordDetectionSettingsResult = new PAMBestRecordDetectionSettingsResult();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettings = new List<PAMBestRecordDetectionSettings>();
            List<PAMBestRecordDetectionSettings> lstPAMBestRecordDetectionSettingsRoot = new List<PAMBestRecordDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<BestRecordDetectionSetting> lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                                                  where c.IsMaster == true
                                                                                  orderby c.OrderOfRules ascending
                                                                                  select c).ToList<BestRecordDetectionSetting>();

                PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettingsRoot = new PAMBestRecordDetectionSettings();
                objPAMBestRecordDetectionSettingsRoot.leaf = false;
                objPAMBestRecordDetectionSettingsRoot.text = "Rules Sequence";
                objPAMBestRecordDetectionSettingsRoot.qtip = "Rules Sequence";
                objPAMBestRecordDetectionSettingsRoot.RuleName = "Rules Sequence";
                objPAMBestRecordDetectionSettingsRoot.id = "1";
                objPAMBestRecordDetectionSettingsRoot.cls = "folder";
                objPAMBestRecordDetectionSettingsRoot.Id = Guid.NewGuid();

                if (lstBestRecordDetectionSetting != null)
                {
                    foreach (BestRecordDetectionSetting objBestRecordDetectionSetting in lstBestRecordDetectionSetting)
                    {
                        PAMBestRecordDetectionSettings objPAMBestRecordDetectionSettings = new PAMBestRecordDetectionSettings();

                        objPAMBestRecordDetectionSettings.Id = objBestRecordDetectionSetting.Id;
                        //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                        //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestRecordDetectionSettings.RuleName = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.RuleParamId = objBestRecordDetectionSetting.RuleParamId;

                        if (objBestRecordDetectionSetting.BestRecordRuleParametersMaster != null)
                            objPAMBestRecordDetectionSettings.RuleParam = objBestRecordDetectionSetting.BestRecordRuleParametersMaster.Parameter;

                        objPAMBestRecordDetectionSettings.RuleId = new Guid(Convert.ToString(objBestRecordDetectionSetting.RuleId));
                        objPAMBestRecordDetectionSettings.RuleEnum = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleEnum;
                        objPAMBestRecordDetectionSettings.Account = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Account);
                        objPAMBestRecordDetectionSettings.Contact = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Contact);
                        objPAMBestRecordDetectionSettings.Lead = Convert.ToBoolean(objBestRecordDetectionSetting.BestRecordDetectionRule.Lead);

                        objPAMBestRecordDetectionSettings.leaf = true;
                        objPAMBestRecordDetectionSettings.cls = "file";
                        objPAMBestRecordDetectionSettings.text = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.qtip = objBestRecordDetectionSetting.BestRecordDetectionRule.RuleName;
                        objPAMBestRecordDetectionSettings.id = objBestRecordDetectionSetting.Id.ToString();
                        lstPAMBestRecordDetectionSettings.Add(objPAMBestRecordDetectionSettings);
                    }

                    objPAMBestRecordDetectionSettingsRoot.children = lstPAMBestRecordDetectionSettings;
                    lstPAMBestRecordDetectionSettingsRoot.Add(objPAMBestRecordDetectionSettingsRoot);

                    objPAMBestRecordDetectionSettingsResult.BestRecordDetectionSettings = lstPAMBestRecordDetectionSettingsRoot;
                    objPAMBestRecordDetectionSettingsResult.Result = true;
                    objPAMBestRecordDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestRecordDetectionSettingsResult;
        }

        public PAMBestRecordRuleParametersMasterResult GetMasterParametersByBestRecordRuleId(string RuleId)
        {
            PAMBestRecordRuleParametersMasterResult obj = new PAMBestRecordRuleParametersMasterResult();
            List<PAMBestRecordRuleParametersMaster> lstPAMBestRecordRuleParametersMaster = new List<PAMBestRecordRuleParametersMaster>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid gRuleId= new Guid(RuleId);
                List<BestRecordRuleParametersMaster> lstBestRecordRuleParametersMaster = (from c in pam2EntitiesContext.BestRecordRuleParameters
                                                                                   where c.RuleId == gRuleId
                                                                                      select c.BestRecordRuleParametersMaster).ToList<BestRecordRuleParametersMaster>();
                if (lstBestRecordRuleParametersMaster != null)
                {
                    foreach (BestRecordRuleParametersMaster objBestRecordRuleParametersMaster in lstBestRecordRuleParametersMaster)
                    {
                        PAMBestRecordRuleParametersMaster objPAMBestRecordRuleParametersMaster = new PAMBestRecordRuleParametersMaster();

                        objPAMBestRecordRuleParametersMaster.Id = objBestRecordRuleParametersMaster.Id;
                        //if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                        //    objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestRecordRuleParametersMaster.Parameter = objBestRecordRuleParametersMaster.Parameter;
                        objPAMBestRecordRuleParametersMaster.ParameterEnum = objBestRecordRuleParametersMaster.ParameterEnum;

                        lstPAMBestRecordRuleParametersMaster.Add(objPAMBestRecordRuleParametersMaster);
                    }

                    obj.PAMBestRecordRuleParametersMasters = lstPAMBestRecordRuleParametersMaster;
                    obj.Result = true;
                    obj.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return obj;
        }

        public List<BestRecordDetectionSetting> GetBestRecordDetectionSettingsOrig()
        {
            List<BestRecordDetectionSetting> lstBestRecordDetectionSetting = new List<BestRecordDetectionSetting>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
               
                lstBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                // join d in pam2EntitiesContext.SessionBestRecordSettings on c.Id equals d.BestRecordDetectionSettingsId
                                                 where c.IsMaster == true 
                                                 orderby c.OrderOfRules ascending
                                                 select c).ToList<BestRecordDetectionSetting>();

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstBestRecordDetectionSetting;
        }

        public bool CheckForRuleDefinedInSession(string RuleName)
        {
            BestRecordDetectionSetting objBestRecordDetectionSetting = new BestRecordDetectionSetting();
            bool bIsRuleDefined = false;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
              //  Guid gSessionID = new Guid(SessionID);

                objBestRecordDetectionSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                // join d in pam2EntitiesContext.SessionBestRecordSettings on c.Id equals d.BestRecordDetectionSettingsId
                                                 where c.IsMaster == true &&
                                                 c.BestRecordDetectionRule.RuleEnum.ToLower().Trim() == RuleName.ToLower().Trim()
                                                 select c).FirstOrDefault<BestRecordDetectionSetting>();
                if (objBestRecordDetectionSetting != null)
                    bIsRuleDefined = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return bIsRuleDefined;
        }

        public ResultSet SaveBestRecordDetectionSettings(List<BestRecordDetectionSetting> lstBestRecordDetectionSetting)
        {
            ResultSet resultSet = new ResultSet();
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                pam2EntitiesContext.BestRecordDetectionSettings.AddRange(lstBestRecordDetectionSetting);
                pam2EntitiesContext.SaveChanges();
                resultSet.Message = "Success";
                resultSet.Result = true;
                resultSet.success = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return resultSet;
        }

        public void DeleteBestRecordDetectionSettings(string SessionID = "0")
        {
            List<BestRecordDetectionSetting> lst = null;
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                if (SessionID == "0")
                {
                     lst = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                            where c.IsMaster == true
                                                            select c).ToList<BestRecordDetectionSetting>();
                }
                else
                {
                    Guid? guidSessionID = new Guid(SessionID);
                    lst = (from c in pam2EntitiesContext.BestRecordDetectionSettings join 
                            d in pam2EntitiesContext.SessionBestRecordSettings on c.Id equals d.BestRecordDetectionSettingsId 
                                                            where c.IsMaster == false && d.SessionId == guidSessionID
                                                             select c).ToList<BestRecordDetectionSetting>();

                     if (lst != null && lst.Count != 0)
                     {
                         List<SessionBestRecordSetting> lstSessionBestRecordSetting = (from c in pam2EntitiesContext.BestRecordDetectionSettings
                                                                                       join d in pam2EntitiesContext.SessionBestRecordSettings on c.Id equals d.BestRecordDetectionSettingsId
                                                                                       where c.IsMaster == false && d.SessionId == guidSessionID
                                                                                       select d).ToList<SessionBestRecordSetting>();

                         if (lstSessionBestRecordSetting != null && lstSessionBestRecordSetting.Count != 0)
                         {
                             pam2EntitiesContext.SessionBestRecordSettings.RemoveRange(lstSessionBestRecordSetting);
                             pam2EntitiesContext.SaveChanges();
                         }
                     }
                }

                if (lst != null && lst.Count != 0)
                {
                    pam2EntitiesContext.BestRecordDetectionSettings.RemoveRange(lst);
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public ResultSet SaveSessionBestRecordSettings(List<BestRecordDetectionSetting> BestRecordDetectionSettings, string SessionID) 
        {
            List<SessionBestRecordSetting> lstSessionBestRecordSetting = new List<SessionBestRecordSetting>();
            Guid? gSessionId = new Guid(SessionID);
            ResultSet resultSet = new ResultSet();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                foreach (BestRecordDetectionSetting objBestRecordDetectionSetting in BestRecordDetectionSettings)
                {
                    SessionBestRecordSetting objSessionBestRecordSetting = new SessionBestRecordSetting();
                    objSessionBestRecordSetting.Id = Guid.NewGuid();
                    objSessionBestRecordSetting.SessionId = gSessionId;
                    objSessionBestRecordSetting.BestRecordDetectionSettingsId = objBestRecordDetectionSetting.Id;
                    objSessionBestRecordSetting.CreatedBy = objBestRecordDetectionSetting.CreatedBy;
                    objSessionBestRecordSetting.CreatedDate = DateTime.UtcNow;
                    lstSessionBestRecordSetting.Add(objSessionBestRecordSetting);
                }

                pam2EntitiesContext.SessionBestRecordSettings.AddRange(lstSessionBestRecordSetting);
                pam2EntitiesContext.SaveChanges();

                resultSet.Message = "Success";
                resultSet.Result = true;
                resultSet.success = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return resultSet;
        }

        public ResultSet UpdateSessionResultStatusByGroup(string SessionId, List<SessionResult> ValidSessionResults, string pamUserId,
            string statusEnumtoSet, string UnprocessedStatusEnum, string ReviewingStatusEnum, string AutoPromotedEnum="",
            string AutoPromotedFillEnum="")
        {
            ResultSet objResultSet = new ResultSet();
          //  string GroupNo = ValidSessionResults[0].GroupNo;
            
            decimal Group = ValidSessionResults[0].GroupNo; // Convert.ToDecimal(GroupNo);

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<SessionResult> lstResults = (from c in pam2EntitiesContext.SessionResults
                                                where c.SessionId == new Guid(SessionId) && c.GroupNo == Group
                                                      select c).ToList<SessionResult>();

                 Status ToSetStatus = pam2EntitiesContext.Status.Where(x => x.Enum == statusEnumtoSet).FirstOrDefault();
                 Status UnprocessedStatus = pam2EntitiesContext.Status.Where(x => x.Enum == UnprocessedStatusEnum).FirstOrDefault();
                 Status ReviewingStatus = pam2EntitiesContext.Status.Where(x => x.Enum == ReviewingStatusEnum).FirstOrDefault();

                 Status AutoPromotedstatus = null;
                 Status AutoPromotedFillStatus = null;

                 if (!String.IsNullOrEmpty(AutoPromotedEnum))
                     AutoPromotedstatus = pam2EntitiesContext.Status.Where(x => x.Enum == AutoPromotedEnum).FirstOrDefault();

                 if (!String.IsNullOrEmpty(AutoPromotedFillEnum))
                     AutoPromotedFillStatus = pam2EntitiesContext.Status.Where(x => x.Enum == AutoPromotedFillEnum).FirstOrDefault();
                

                 foreach(SessionResult sessionResultTemp in lstResults)
                 {
                    SessionResult sessionResult = pam2EntitiesContext.SessionResults.Where(s => s.SessionResultId == sessionResultTemp.SessionResultId).FirstOrDefault();
                    if (ValidSessionResults.Contains(sessionResult) || sessionResult.ReviewStatus.Value == UnprocessedStatus.StatusId || 
                        sessionResult.ReviewStatus.Value == ReviewingStatus.StatusId ||
                        (AutoPromotedstatus != null && sessionResult.ReviewStatus.Value == AutoPromotedstatus.StatusId) ||
                        (AutoPromotedFillStatus != null && sessionResult.ReviewStatus.Value == AutoPromotedFillStatus.StatusId) )
                    {
                        sessionResult.Reviewer = new Guid(pamUserId);
                        sessionResult.PunchIn = DateTime.UtcNow;
                        sessionResult.PunchOut = DateTime.UtcNow;

                        if (ToSetStatus != null)
                            sessionResult.ReviewStatus = ToSetStatus.StatusId;
                        sessionResult.CurentStatusDateTime = DateTime.UtcNow;
                        pam2EntitiesContext.SaveChanges();
                    }
                    //else if(sessionResult.ReviewStatus == UnprocessedStatus)
                    //{
                    //    sessionResult.Reviewer = new Guid(pamUserId);
                    //    sessionResult.PunchIn = DateTime.UtcNow;
                    //    sessionResult.PunchOut = DateTime.UtcNow;

                    //    if (autoAcceptedStatus != null)
                    //        sessionResult.ReviewStatus = autoAcceptedStatus.StatusId;
                    //    sessionResult.CurentStatusDateTime = DateTime.UtcNow;
                    //    pam2EntitiesContext.SaveChanges();
                    //}
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objResultSet;
        }


        #endregion

        #region Best Field Detection

        public PAMBestFieldDetectionSettingsResult GetBestFieldDetectionRules()
        {
            PAMBestFieldDetectionSettingsResult objPAMBestFieldDetectionSettingsResult = new PAMBestFieldDetectionSettingsResult();
            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettings = new List<PAMBestFieldDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<BestFieldDetectionRule> lstBestFieldDetectionRule = (from c in pam2EntitiesContext.BestFieldDetectionRules
                                                                          select c).ToList<BestFieldDetectionRule>();
                if (lstBestFieldDetectionRule != null)
                {
                    foreach (BestFieldDetectionRule objBestFieldDetectionRule in lstBestFieldDetectionRule)
                    {
                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettings = new PAMBestFieldDetectionSettings();
                        objPAMBestFieldDetectionSettings.Id = objBestFieldDetectionRule.Id.ToString();
                        objPAMBestFieldDetectionSettings.RuleEnum = objBestFieldDetectionRule.RuleEnum;
                        objPAMBestFieldDetectionSettings.RuleName = objBestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.BestFieldDetectionRuleTypesId = objBestFieldDetectionRule.BestFieldDetectionRuleTypesId;
                        objPAMBestFieldDetectionSettings.RuleTypeEnum = objBestFieldDetectionRule.BestFieldDetectionRuleType.RuleTypeEnum;
                        objPAMBestFieldDetectionSettings.RuleId = objBestFieldDetectionRule.Id;

                        //objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionRule.Account);
                        //objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionRule.Contact);
                        //objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionRule.Lead);

                        objPAMBestFieldDetectionSettings.leaf = true;
                        objPAMBestFieldDetectionSettings.cls = "file";
                        objPAMBestFieldDetectionSettings.text = objBestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.qtip = objBestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.id = objBestFieldDetectionRule.Id.ToString();
                        lstPAMBestFieldDetectionSettings.Add(objPAMBestFieldDetectionSettings);
                    }

                    objPAMBestFieldDetectionSettingsResult.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettings;
                    objPAMBestFieldDetectionSettingsResult.Result = true;
                    objPAMBestFieldDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestFieldDetectionSettingsResult;
        }

        public PAMBestFieldDetectionSettingsResult GetBestFieldDetectionSettings()
        {
            PAMBestFieldDetectionSettingsResult objPAMBestFieldDetectionSettingsResult = new PAMBestFieldDetectionSettingsResult();

            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettings = new List<PAMBestFieldDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<BestFieldDetectionSetting> lstBestFieldDetectionSetting = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                                                where c.IsMaster == true
                                                                                orderby c.OrderOfRules ascending
                                                                                select c).ToList<BestFieldDetectionSetting>();

                //objPAMBestFieldDetectionSettingsRoot.leaf = false;
                //objPAMBestFieldDetectionSettingsRoot.text = "Rules Sequence";
                //objPAMBestFieldDetectionSettingsRoot.qtip = "Rules Sequence";
                //objPAMBestFieldDetectionSettingsRoot.RuleName = "Rules Sequence";
                //objPAMBestFieldDetectionSettingsRoot.id = "1";
                //objPAMBestFieldDetectionSettingsRoot.cls = "folder";
                //objPAMBestFieldDetectionSettingsRoot.Id = Convert.ToString(Guid.NewGuid());

                if (lstBestFieldDetectionSetting != null)
                {
                    foreach (BestFieldDetectionSetting objBestFieldDetectionSetting in lstBestFieldDetectionSetting)
                    {
                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettings = new PAMBestFieldDetectionSettings();

                        objPAMBestFieldDetectionSettings.Id = objBestFieldDetectionSetting.Id.ToString();

                        //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                        //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestFieldDetectionSettings.RuleName = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.RuleParamId = objBestFieldDetectionSetting.RuleParamId;
                        objPAMBestFieldDetectionSettings.RuleTypeEnum = objBestFieldDetectionSetting.BestFieldDetectionRule.BestFieldDetectionRuleType.RuleTypeEnum;
                        objPAMBestFieldDetectionSettings.BestFieldDetectionRuleTypesId = objBestFieldDetectionSetting.BestFieldDetectionRule.BestFieldDetectionRuleTypesId;

                        if (objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster != null)
                            objPAMBestFieldDetectionSettings.RuleParam = objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster.Parameter;

                        //  objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId = objBestFieldDetectionSetting.BestFieldDetGroupMasterId;
                        //    objPAMBestFieldDetectionSettings.BestFieldDetPicklistFieldsId = objBestFieldDetectionSetting.BestFieldDetPicklistFieldsId;
                        //  objPAMBestFieldDetectionSettings.GroupName = objBestFieldDetectionSetting.GroupName;

                        objPAMBestFieldDetectionSettings.RuleId = new Guid(Convert.ToString(objBestFieldDetectionSetting.RuleId));
                        objPAMBestFieldDetectionSettings.RuleEnum = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleEnum;
                        objPAMBestFieldDetectionSettings.SectionId = objBestFieldDetectionSetting.SectionId;
                        objPAMBestFieldDetectionSettings.AttributeSettingId = objBestFieldDetectionSetting.AttributeSettingId;

                        if (objPAMBestFieldDetectionSettings.RuleEnum.ToLower().Trim() == "hierarchyvalues")
                        {
                            List<BestFieldDetPicklistFieldDetail> lstBestFieldDetPicklistFieldDetail =  objBestFieldDetectionSetting.BestFieldDetPicklistFieldDetails.ToList<BestFieldDetPicklistFieldDetail>();
                            lstBestFieldDetPicklistFieldDetail =  lstBestFieldDetPicklistFieldDetail.OrderBy(c => c.Order).ToList<BestFieldDetPicklistFieldDetail>();

                            List<HierarchyOfPickListFields> lstHierarchyOfPickListFields = new List<HierarchyOfPickListFields>();
                            foreach (var obj in lstBestFieldDetPicklistFieldDetail)
                            {
                                HierarchyOfPickListFields objHierarchyOfPickListFields = new HierarchyOfPickListFields();
                                
                                objHierarchyOfPickListFields.Label = obj.Label;
                                objHierarchyOfPickListFields.Score = Convert.ToString(obj.Order);
                                objHierarchyOfPickListFields.Value = obj.Value;
                                lstHierarchyOfPickListFields.Add(objHierarchyOfPickListFields);
                            }

                            objPAMBestFieldDetectionSettings.HierarchyRuleRecords = lstHierarchyOfPickListFields;

                            //  objPAMBestFieldDetectionSettings.PickListFieldSchemaName = objBestFieldDetectionSetting.BestFieldDetPicklistField.PicklistFieldSchema;

                            //   objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Account);
                            //   objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Contact);
                            //   objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Lead);

                            /*    if (objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails != null)
                                {
                                    List<string> list = (from c in objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails
                                                         orderby c.Order ascending
                                                         select c.Label
                                                         ).ToList<string>();

                                    string Params = string.Join(", ", list.ToArray());

                                    objPAMBestFieldDetectionSettings.RuleParam = Params;
                                    List<HierarchyOfPickListFields> lstHierarchyOfPickListFields = new List<HierarchyOfPickListFields>();

                                    var listSorted = objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails.OrderBy(c=>c.Order).ToList();;
                                    foreach (var obj in listSorted)
                                    {
                                        HierarchyOfPickListFields objHierarchyOfPickListFields = new HierarchyOfPickListFields();
                                        objHierarchyOfPickListFields.Label = obj.Label;
                                        objHierarchyOfPickListFields.Score = Convert.ToString(obj.Order);
                                        objHierarchyOfPickListFields.Value = obj.Value;
                                        lstHierarchyOfPickListFields.Add(objHierarchyOfPickListFields);
                                    }

                                    objPAMBestFieldDetectionSettings.HierarchyRuleRecords = lstHierarchyOfPickListFields;
                                    int count = lstHierarchyOfPickListFields.Count;

                                    List<PickListScore> lstPickListScore = new List<PickListScore>();
                                    PickListScore objPickListScore = new PickListScore();

                                    for (int i = 1; i <= count; i++)
                                    {
                                        objPickListScore = new PickListScore();
                                        objPickListScore.ScoreValue = i;
                                        objPickListScore.ScoreText = i.ToString();
                                        lstPickListScore.Add(objPickListScore);
                                    }

                                    objPAMBestFieldDetectionSettings.PickListScoreRecords = lstPickListScore;
                                }  */
                        }
                        else// if (objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId != null)
                        {
                            //objPAMBestFieldDetectionSettings.Account = false;
                            //objPAMBestFieldDetectionSettings.Contact = false;
                            //objPAMBestFieldDetectionSettings.Lead = false;

                            //     var list = objBestFieldDetectionSetting.BestFieldDetGroupMaster.BestFieldDetGroupEntitywises;

                            /*    foreach(var obj in list)
                                {
                                    if(obj.EntitySetting.EntityLogicalName.ToLower()=="account")
                                        objPAMBestFieldDetectionSettings.Account = true;
                                    if(obj.EntitySetting.EntityLogicalName.ToLower()=="contact")
                                        objPAMBestFieldDetectionSettings.Contact = true;
                                    if(obj.EntitySetting.EntityLogicalName.ToLower()=="lead")
                                        objPAMBestFieldDetectionSettings.Lead = true;
                                }  */
                        }
                      //  else
                        {
                            //objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Account);
                            //objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Contact);
                            //objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Lead);
                        }

                        objPAMBestFieldDetectionSettings.id = objBestFieldDetectionSetting.Id.ToString();
                        objPAMBestFieldDetectionSettings.text = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.qtip = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.leaf = true;
                        objPAMBestFieldDetectionSettings.cls = "file";

                        lstPAMBestFieldDetectionSettings.Add(objPAMBestFieldDetectionSettings);
                    }

                }

              //  objPAMBestFieldDetectionSettingsRoot.children = lstPAMBestFieldDetectionSettings;
              //  lstPAMBestFieldDetectionSettingsRoot.Add(objPAMBestFieldDetectionSettingsRoot);

                objPAMBestFieldDetectionSettingsResult.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettings;
                objPAMBestFieldDetectionSettingsResult.Result = true;
                objPAMBestFieldDetectionSettingsResult.success = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestFieldDetectionSettingsResult;
        }

        public List<BestFieldDetectionSetting> GetBestFieldDetectionSettingsOrig(string EntitySettingsID)
        {
            List<BestFieldDetectionSetting> lstBestFieldDetectionSetting = new List<BestFieldDetectionSetting>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid gEntitySettingsID = new Guid(EntitySettingsID);

                lstBestFieldDetectionSetting = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                 // join d in pam2EntitiesContext.SessionBestRecordSettings on c.Id equals d.BestRecordDetectionSettingsId
                                                where c.IsMaster == true &&
                                                (c.AttributeSetting.EntitySetting.EntitySettingId == gEntitySettingsID ||
                                                c.Section.EntitySettingId == gEntitySettingsID)
                                                orderby c.AttributeSetting.SchemaName ascending, c.OrderOfRules ascending
                                                select c).ToList<BestFieldDetectionSetting>();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstBestFieldDetectionSetting;

        }

        public PAMBestFieldDetectionSettingsResult GetBestFieldDetectionSettingsRuleOrGroupWise(string Id, bool IsGroup)
        {
            PAMBestFieldDetectionSettingsResult objPAMBestFieldDetectionSettingsResult = new PAMBestFieldDetectionSettingsResult();
            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettings = new List<PAMBestFieldDetectionSettings>();
            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettingsRoot = new List<PAMBestFieldDetectionSettings>();
            PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettingsRoot = new PAMBestFieldDetectionSettings();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                List<BestFieldDetectionSetting> lstBestFieldDetectionSetting = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                                                where c.IsMaster == true
                                                                                orderby c.OrderOfRules ascending
                                                                                select c).ToList<BestFieldDetectionSetting>();
                Guid gId = new Guid(Id);

                if(IsGroup)
                {
                  lstBestFieldDetectionSetting =  lstBestFieldDetectionSetting.Where(c => c.SectionId == gId).ToList<BestFieldDetectionSetting>();
                }
                else
                {
                   lstBestFieldDetectionSetting = lstBestFieldDetectionSetting.Where(c => c.AttributeSettingId == gId).ToList<BestFieldDetectionSetting>();
                }


                if (lstBestFieldDetectionSetting != null)
                {
                    foreach (BestFieldDetectionSetting objBestFieldDetectionSetting in lstBestFieldDetectionSetting)
                    {
                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettings = new PAMBestFieldDetectionSettings();

                        objPAMBestFieldDetectionSettings.Id = objBestFieldDetectionSetting.Id.ToString();

                        //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                        //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestFieldDetectionSettings.RuleName = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.RuleParamId = objBestFieldDetectionSetting.RuleParamId;

                        if (objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster != null)
                            objPAMBestFieldDetectionSettings.RuleParam = objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster.Parameter;

                        //  objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId = objBestFieldDetectionSetting.BestFieldDetGroupMasterId;
                        //    objPAMBestFieldDetectionSettings.BestFieldDetPicklistFieldsId = objBestFieldDetectionSetting.BestFieldDetPicklistFieldsId;
                        //  objPAMBestFieldDetectionSettings.GroupName = objBestFieldDetectionSetting.GroupName;
                        objPAMBestFieldDetectionSettings.RuleId = new Guid(Convert.ToString(objBestFieldDetectionSetting.RuleId));
                        objPAMBestFieldDetectionSettings.RuleEnum = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleEnum;
                        objPAMBestFieldDetectionSettings.SectionId = objBestFieldDetectionSetting.SectionId;
                        objPAMBestFieldDetectionSettings.AttributeSettingId = objBestFieldDetectionSetting.AttributeSettingId;

                        if (objPAMBestFieldDetectionSettings.BestFieldDetPicklistFieldsId != null)
                        {
                            //  objPAMBestFieldDetectionSettings.PickListFieldSchemaName = objBestFieldDetectionSetting.BestFieldDetPicklistField.PicklistFieldSchema;

                            //   objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Account);
                            //   objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Contact);
                            //   objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Lead);

                            /*    if (objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails != null)
                                {
                                    List<string> list = (from c in objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails
                                                         orderby c.Order ascending
                                                         select c.Label
                                                         ).ToList<string>();

                                    string Params = string.Join(", ", list.ToArray());

                                    objPAMBestFieldDetectionSettings.RuleParam = Params;
                                    List<HierarchyOfPickListFields> lstHierarchyOfPickListFields = new List<HierarchyOfPickListFields>();

                                    var listSorted = objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails.OrderBy(c=>c.Order).ToList();;
                                    foreach (var obj in listSorted)
                                    {
                                        HierarchyOfPickListFields objHierarchyOfPickListFields = new HierarchyOfPickListFields();
                                        objHierarchyOfPickListFields.Label = obj.Label;
                                        objHierarchyOfPickListFields.Score = Convert.ToString(obj.Order);
                                        objHierarchyOfPickListFields.Value = obj.Value;
                                        lstHierarchyOfPickListFields.Add(objHierarchyOfPickListFields);
                                    }

                                    objPAMBestFieldDetectionSettings.HierarchyRuleRecords = lstHierarchyOfPickListFields;
                                    int count = lstHierarchyOfPickListFields.Count;

                                    List<PickListScore> lstPickListScore = new List<PickListScore>();
                                    PickListScore objPickListScore = new PickListScore();

                                    for (int i = 1; i <= count; i++)
                                    {
                                        objPickListScore = new PickListScore();
                                        objPickListScore.ScoreValue = i;
                                        objPickListScore.ScoreText = i.ToString();
                                        lstPickListScore.Add(objPickListScore);
                                    }

                                    objPAMBestFieldDetectionSettings.PickListScoreRecords = lstPickListScore;
                                }  */
                        }
                       // else if (objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId != null)
                      //  {
                            //objPAMBestFieldDetectionSettings.Account = false;
                            //objPAMBestFieldDetectionSettings.Contact = false;
                            //objPAMBestFieldDetectionSettings.Lead = false;

                            //     var list = objBestFieldDetectionSetting.BestFieldDetGroupMaster.BestFieldDetGroupEntitywises;

                            /*    foreach(var obj in list)
                                {
                                    if(obj.EntitySetting.EntityLogicalName.ToLower()=="account")
                                        objPAMBestFieldDetectionSettings.Account = true;
                                    if(obj.EntitySetting.EntityLogicalName.ToLower()=="contact")
                                        objPAMBestFieldDetectionSettings.Contact = true;
                                    if(obj.EntitySetting.EntityLogicalName.ToLower()=="lead")
                                        objPAMBestFieldDetectionSettings.Lead = true;
                                }  */
                       // }
                        else
                        {
                            objPAMBestFieldDetectionSettings.id = objBestFieldDetectionSetting.Id.ToString();
                            objPAMBestFieldDetectionSettings.text = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                            objPAMBestFieldDetectionSettings.qtip = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                            objPAMBestFieldDetectionSettings.leaf = true;
                            objPAMBestFieldDetectionSettings.cls = "file";

                            //objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Account);
                            //objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Contact);
                            //objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Lead);
                        }

                        lstPAMBestFieldDetectionSettings.Add(objPAMBestFieldDetectionSettings);
                    }

                    objPAMBestFieldDetectionSettingsRoot.children = lstPAMBestFieldDetectionSettings;
                    lstPAMBestFieldDetectionSettingsRoot.Add(objPAMBestFieldDetectionSettingsRoot);

                    objPAMBestFieldDetectionSettingsResult.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettingsRoot;
                    objPAMBestFieldDetectionSettingsResult.Result = true;
                    objPAMBestFieldDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestFieldDetectionSettingsResult;
        }

        public PAMBestFieldRuleParametersMasterResult GetMasterParametersByBestFieldRuleId(string RuleId)
        {
            PAMBestFieldRuleParametersMasterResult obj = new PAMBestFieldRuleParametersMasterResult();
            List<PAMBestFieldRuleParametersMaster> lstPAMBestFieldRuleParametersMaster = new List<PAMBestFieldRuleParametersMaster>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                Guid gRuleId = new Guid(RuleId);

                List<BestFieldsDetRuleParametersMaster> lstBestFieldsDetRuleParametersMaster = (from c in pam2EntitiesContext.BestFieldDetRuleParameters
                                                                                        where c.RuleId == gRuleId
                                                                                        select c.BestFieldsDetRuleParametersMaster).ToList<BestFieldsDetRuleParametersMaster>();
                if (lstBestFieldsDetRuleParametersMaster != null)
                {
                    foreach (BestFieldsDetRuleParametersMaster objBestFieldsDetRuleParametersMaster in lstBestFieldsDetRuleParametersMaster)
                    {
                        PAMBestFieldRuleParametersMaster objPAMBestFieldRuleParametersMaster = new PAMBestFieldRuleParametersMaster();

                        objPAMBestFieldRuleParametersMaster.Id = objBestFieldsDetRuleParametersMaster.Id;
                        //if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                        //    objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestFieldRuleParametersMaster.Parameter = objBestFieldsDetRuleParametersMaster.Parameter;
                        objPAMBestFieldRuleParametersMaster.ParameterEnum = objBestFieldsDetRuleParametersMaster.ParameterEnum;

                        lstPAMBestFieldRuleParametersMaster.Add(objPAMBestFieldRuleParametersMaster);
                    }

                    obj.PAMBestFieldRuleParametersMasters = lstPAMBestFieldRuleParametersMaster;
                    obj.Result = true;
                    obj.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return obj;
        }

        public List<BestFieldsDetRuleParametersMaster> GetAllFieldDetRuleParameterMasters()
        {
            List<BestFieldsDetRuleParametersMaster> lstBestFieldsDetRuleParametersMaster = null;

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                lstBestFieldsDetRuleParametersMaster = (from c in pam2EntitiesContext.BestFieldsDetRuleParametersMasters
                                                                                                select c).ToList<BestFieldsDetRuleParametersMaster>();


            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstBestFieldsDetRuleParametersMaster;
        }

        public ResultSet SaveBestFieldDetectionSettings(List<BestFieldDetectionSetting> lstBestFieldDetectionSetting)
        {
            ResultSet resultSet = new ResultSet();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                pam2EntitiesContext.BestFieldDetectionSettings.AddRange(lstBestFieldDetectionSetting);
                pam2EntitiesContext.SaveChanges();
                resultSet.Message = "Success";
                resultSet.Result = true;
                resultSet.success = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return resultSet;
        }

        public void DeleteBestFieldDetectionSettings(string SessionID = "0", string AttributeSettingsId = "",
            string SectionId = "")
        {
            List<BestFieldDetectionSetting> lst = new List<BestFieldDetectionSetting>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                if (SessionID == "0")
                {
                    Guid gId = new Guid();
                    if (!String.IsNullOrEmpty(AttributeSettingsId))
                    {
                        gId = new Guid(AttributeSettingsId);
                        lst = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                               where c.IsMaster == true && c.AttributeSettingId == gId
                               select c).ToList<BestFieldDetectionSetting>();
                    }

                    if (!String.IsNullOrEmpty(SectionId))
                    {
                        gId = new Guid(SectionId);
                        List<BestFieldDetectionSetting> lstTemp = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                               where c.IsMaster == true && c.SectionId == gId
                               select c).ToList<BestFieldDetectionSetting>();

                        if (lstTemp != null || lstTemp.Count > 0)
                        {
                            lst.AddRange(lstTemp);
                        }
                    }
                }
                else
                {
                    
                }

                if (lst != null && lst.Count != 0)
                {
                    pam2EntitiesContext.BestFieldDetectionSettings.RemoveRange(lst);
                    pam2EntitiesContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

//        public PAMBestField_FieldGroupsDetailResultSet GetBestField_FieldGroupsDetail(string EntitySettingsID)
//        {
//            PAMBestField_FieldGroupsDetailResultSet objPAMBestField_FieldGroupsDetailResultSet = new PAMBestField_FieldGroupsDetailResultSet();
//            List<PAMBestField_FieldGroupsDetail> lstPAMBestField_FieldGroupsDetail = new List<PAMBestField_FieldGroupsDetail>();
//            SqlDataReader dr = null;

//            try
//            {
//                if (_connection == null)
//                {
//                    _connection = new SqlConnection(sqlConnString);
//                }

//                if (_connection != null && _connection.State != System.Data.ConnectionState.Open)
//                {
//                    _connection.Open();
//                }

//                using (SqlCommand cmd = _connection.CreateCommand())
//                {
//                    Guid? gEntitySettingsID = new Guid(EntitySettingsID);

//                    //List<BestFieldDetGroupEntitywiseDetail> lstBestFieldDetGroupEntitywiseDetail = (from c in pam2EntitiesContext.BestFieldDetGroupEntitywiseDetails
//                    //                                                                where c.BestFieldDetGroupEntitywise.EntitySettingId == gEntitySettingsID
//                    //                                                                orderby c.BestFieldDetGroupEntitywise.BestFieldDetGroupMasterId ascending
//                    //                                                                select c).ToList<BestFieldDetGroupEntitywiseDetail>();

//                    cmd.CommandText = @"select GEDetail.Id, GEntity.BestFieldDetGroupMasterId, GM.FieldGroupName, GEDetail.FieldDisplayName, GEDetail.FieldSchemaName
//                                    from
//                                    [dbo].[BestFieldDetGroupMaster] GM left join [dbo].[BestFieldDetGroupEntitywise] GEntity on GM.Id = GEntity.BestFieldDetGroupMasterId
//		                            left join [dbo].[BestFieldDetGroupEntitywiseDetail] GEDetail on  GEntity.Id = GEDetail.BestFieldDetGroupEntitywiseId
//		                            where GEntity.EntitySettingId = '" + EntitySettingsID.Trim() + "'" +
//                                        " order by GM.FieldGroupName, GEDetail.FieldDisplayName";

//                    dr = cmd.ExecuteReader();
//                    PAMBestField_FieldGroupsDetail FieldGroupsObj = new PAMBestField_FieldGroupsDetail();

//                    List<PAMBestField_FieldGroupsDetail> lstFields = new List<PAMBestField_FieldGroupsDetail>();
//                    string strPreviousGroup = String.Empty;
//                    int i = 1;

//                    if (dr.HasRows)
//                    {
//                        while (dr.Read())
//                        {
//                          //  if (obj == null)
//                            //    continue;

//                            PAMBestField_FieldGroupsDetail objField = new PAMBestField_FieldGroupsDetail();

//                            //    if ((String.IsNullOrEmpty(strPreviousGroup) || (obj.BestFieldDetGroupEntitywise.BestFieldDetGroupMasterId != null && String.Compare(strPreviousGroup, Convert.ToString(dr["BestFieldDetGroupMasterId"])) != 0))
//                            if (String.IsNullOrEmpty(strPreviousGroup) || String.Compare(strPreviousGroup, Convert.ToString(dr["BestFieldDetGroupMasterId"])) != 0)
//                            {
//                                if (!String.IsNullOrEmpty(strPreviousGroup))
//                                {
//                                    FieldGroupsObj.leaf = false;
//                                    lstPAMBestField_FieldGroupsDetail.Add(FieldGroupsObj);
//                                    FieldGroupsObj = new PAMBestField_FieldGroupsDetail();
//                                    FieldGroupsObj.children = new List<PAMBestField_FieldGroupsDetail>();
//                                }

//                                lstFields = new List<PAMBestField_FieldGroupsDetail>();

//                                FieldGroupsObj.leaf = false;
//                                FieldGroupsObj.text = Convert.ToString(dr["FieldGroupName"]);
//                                FieldGroupsObj.cls = "folder";
//                                FieldGroupsObj.EntitySettingId = EntitySettingsID;
//                                FieldGroupsObj.FieldGroupName = Convert.ToString(dr["FieldGroupName"]);
//                                FieldGroupsObj.id = i++.ToString();

//                                FieldGroupsObj.BestFieldDetGroupMasterId = Convert.ToString(dr["BestFieldDetGroupMasterId"]);
//                                FieldGroupsObj.qtip = Convert.ToString(dr["FieldGroupName"]);
//                                FieldGroupsObj.FieldDisplayName = Convert.ToString(dr["FieldGroupName"]);
//                                FieldGroupsObj.FieldSchemaName = "";
//                                //  FieldGroupsObj.BestFieldDetGroupEntitywiseDetailId =  Convert.ToString(dr["Id"]);

//                            }

//                            if (Convert.ToString(dr["Id"]) != String.Empty)
//                            {
//                                objField.id = Convert.ToString(dr["Id"]);
//                                objField.FieldDisplayName = Convert.ToString(dr["FieldDisplayName"]);
//                                objField.FieldSchemaName = Convert.ToString(dr["FieldSchemaName"]);
//                                objField.BestFieldDetGroupMasterId = Convert.ToString(dr["BestFieldDetGroupMasterId"]);
//                                objField.text = Convert.ToString(dr["FieldDisplayName"]);

//                                objField.qtip = Convert.ToString(dr["FieldDisplayName"]);
//                                objField.cls = "file";
//                                objField.leaf = true;


//                                lstFields.Add(objField);
//                                FieldGroupsObj.children = lstFields;
//                            }

//                            strPreviousGroup = Convert.ToString(dr["BestFieldDetGroupMasterId"]);
//                        }

//                        FieldGroupsObj.leaf = false;
//                        lstPAMBestField_FieldGroupsDetail.Add(FieldGroupsObj);
//                    }
//                }

//                dr.Close();

//                if (_connection.State != ConnectionState.Closed)
//                {
//                    _connection.Close();
//                }

//                objPAMBestField_FieldGroupsDetailResultSet.Message = "Success";
//                objPAMBestField_FieldGroupsDetailResultSet.FieldGroupsDetail = lstPAMBestField_FieldGroupsDetail;
//                objPAMBestField_FieldGroupsDetailResultSet.Result = true;


               
                     


//            }
//            catch (Exception ex)
//            {
//                if (_connection.State != ConnectionState.Closed)
//                {
//                    _connection.Close();
//                }

//                objPAMBestField_FieldGroupsDetailResultSet.Message = ex.Message;
//                objPAMBestField_FieldGroupsDetailResultSet.Result = false;
//            }

//            return objPAMBestField_FieldGroupsDetailResultSet;
//        }

//        public PAMBestFieldGroupMasterResult GetBestFieldGroups()
//        {
//            PAMBestFieldGroupMasterResult objPAMBestFieldGroupMasterResult = null;
//            List<PAMBestFieldGroupMaster> lstPAMBestFieldGroupMaster= null;

//            try
//            {
//                objPAMBestFieldGroupMasterResult = new PAMBestFieldGroupMasterResult();
//                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

//                List<BestFieldDetGroupMaster> lstBestFieldGroupMaster = (from c in pam2EntitiesContext.BestFieldDetGroupMasters
//                                                                            select c).ToList<BestFieldDetGroupMaster>();

//                List<BestFieldDetGroupEntitywise> lstBestFieldDetGroupEntitywise = (from c in pam2EntitiesContext.BestFieldDetGroupEntitywises
//                                                                         select c).ToList<BestFieldDetGroupEntitywise>();


//                lstPAMBestFieldGroupMaster = new List<PAMBestFieldGroupMaster>();

//                foreach (var obj in lstBestFieldGroupMaster)
//                {
//                    var lst1 = lstBestFieldDetGroupEntitywise.Where(c => c.BestFieldDetGroupMasterId == obj.Id).ToList();
//                    bool Account = false; bool Contact = false; bool Lead = false;

//                    var Acc = lst1.Where(c => c.EntitySetting.EntityLogicalName.ToLower() == "account").FirstOrDefault();
//                    if(Acc !=null)
//                    {
//                        Account = true;
//                    }

//                    var Cont = lst1.Where(c => c.EntitySetting.EntityLogicalName.ToLower() == "contact").FirstOrDefault();
//                    if (Cont != null)
//                    {
//                        Contact = true;
//                    }

//                    var varLead = lst1.Where(c => c.EntitySetting.EntityLogicalName.ToLower() == "lead").FirstOrDefault();
//                    if (varLead != null)
//                    {
//                        Lead = true;
//                    }

//                    PAMBestFieldGroupMaster objPAMBestFieldGroupMaster = new PAMBestFieldGroupMaster();
//                    objPAMBestFieldGroupMaster.Account = Account;
//                    objPAMBestFieldGroupMaster.Contact = Contact;
//                    objPAMBestFieldGroupMaster.Lead = Lead;
//                    objPAMBestFieldGroupMaster.Account = Account;
//                    objPAMBestFieldGroupMaster.Id = obj.Id;
//                    objPAMBestFieldGroupMaster.FieldGroupName = obj.FieldGroupName;
//                    lstPAMBestFieldGroupMaster.Add(objPAMBestFieldGroupMaster);
//                }

//                objPAMBestFieldGroupMasterResult.BestFieldGroups = lstPAMBestFieldGroupMaster;
//                objPAMBestFieldGroupMasterResult.Result = true;
//                objPAMBestFieldGroupMasterResult.success = true;

//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }

//            return objPAMBestFieldGroupMasterResult;
//        }

//        public PAMBestFieldGroupMasterResult GetBestFieldGroupsByEntity(string EntitySettingsID)
//        {
//            PAMBestFieldGroupMasterResult objPAMBestFieldGroupMasterResult = null;
//            List<PAMBestFieldGroupMaster> lstPAMBestFieldGroupMaster= null;

//            try
//            {
//                Guid gEntitySettingsID = new Guid(EntitySettingsID);

//                objPAMBestFieldGroupMasterResult = new PAMBestFieldGroupMasterResult();
//                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

//                //List<BestFieldDetGroupMaster> lstBestFieldGroupMaster = (from c in pam2EntitiesContext.BestFieldDetGroupMasters
//                //                                                            select c).ToList<BestFieldDetGroupMaster>();

//                List<BestFieldDetGroupMaster> lstBestFieldGroupMaster = (from c in pam2EntitiesContext.BestFieldDetGroupEntitywises
//                                                                                    join d in pam2EntitiesContext.BestFieldDetGroupMasters 
//                                                                                    on c.BestFieldDetGroupMasterId equals d.Id
//                                                                                    where c.EntitySettingId == gEntitySettingsID
//                                                                                    select d).ToList<BestFieldDetGroupMaster>();


//                lstPAMBestFieldGroupMaster = new List<PAMBestFieldGroupMaster>();

//                foreach (var obj in lstBestFieldGroupMaster)
//                {
//                    //var lst1 = lstBestFieldDetGroupEntitywise.Where(c => c.BestFieldDetGroupMasterId == obj.Id).ToList();
//                    //bool Account = false; bool Contact = false; bool Lead = false;

//                    //var Acc = lst1.Where(c => c.EntitySetting.EntityLogicalName.ToLower() == "account").FirstOrDefault();
//                    //if(Acc !=null)
//                    //{
//                    //    Account = true;
//                    //}

//                    //var Cont = lst1.Where(c => c.EntitySetting.EntityLogicalName.ToLower() == "contact").FirstOrDefault();
//                    //if (Cont != null)
//                    //{
//                    //    Contact = true;
//                    //}

//                    //var varLead = lst1.Where(c => c.EntitySetting.EntityLogicalName.ToLower() == "lead").FirstOrDefault();
//                    //if (varLead != null)
//                    //{
//                    //    Lead = true;
//                    //}

//                    PAMBestFieldGroupMaster objPAMBestFieldGroupMaster = new PAMBestFieldGroupMaster();
//                  //  objPAMBestFieldGroupMaster.Account = Account;
//                  //  objPAMBestFieldGroupMaster.Contact = Contact;
//                  //  objPAMBestFieldGroupMaster.Lead = Lead;
//                  //  objPAMBestFieldGroupMaster.Account = Account;
//                    objPAMBestFieldGroupMaster.Id = obj.Id;
//                    objPAMBestFieldGroupMaster.FieldGroupName = obj.FieldGroupName;
//                    lstPAMBestFieldGroupMaster.Add(objPAMBestFieldGroupMaster);
//                }

//                objPAMBestFieldGroupMasterResult.BestFieldGroups = lstPAMBestFieldGroupMaster;
//                objPAMBestFieldGroupMasterResult.Result = true;
//                objPAMBestFieldGroupMasterResult.success = true;

//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }

//            return objPAMBestFieldGroupMasterResult;
//        }

        
        //public ResultSet AddFieldGroup(BestFieldDetGroupMaster bestFieldDetGroupMaster)
        //{
        //    ResultSet resultSet = new ResultSet();

        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        pam2EntitiesContext.BestFieldDetGroupMasters.Add(bestFieldDetGroupMaster);
        //        pam2EntitiesContext.SaveChanges();
        //        resultSet.Message = "Success";
        //        resultSet.Result = true;
        //        resultSet.success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    return resultSet;
        //}

        //public ResultSet UpdateFieldGroup(string Id, string GroupName, string updatedBy)
        //{
        //    ResultSet objResult = new ResultSet();
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        BestFieldDetGroupMaster objBestFieldDetGroupMaster = null;
        //        Guid IdGuid = new Guid(Id);
        //        objBestFieldDetGroupMaster = pam2EntitiesContext.BestFieldDetGroupMasters.Where(s => s.Id == IdGuid).FirstOrDefault();

        //        if (objBestFieldDetGroupMaster != null)
        //        {
        //            objBestFieldDetGroupMaster.FieldGroupName = GroupName;
        //            objBestFieldDetGroupMaster.UpdatedDate = DateTime.UtcNow;
        //            objBestFieldDetGroupMaster.UpdatedBy = new Guid(updatedBy);
        //            int count = pam2EntitiesContext.SaveChanges();

        //            objResult.Message = "Success";
        //            objResult.Result = true;
        //        }
        //    }
        //    catch (Exception excObj)
        //    {
        //        throw excObj;
        //    }
        //    return objResult;
        //}

        //public ResultSet DeleteFieldGroup(string Id)
        //{
        //    ResultSet objResult = new ResultSet();
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        BestFieldDetGroupMaster objBestFieldDetGroupMaster = null;
        //        Guid IdGuid = new Guid(Id);

        //        objBestFieldDetGroupMaster = pam2EntitiesContext.BestFieldDetGroupMasters.Where(s => s.Id == IdGuid).FirstOrDefault<BestFieldDetGroupMaster>();
        //        pam2EntitiesContext.BestFieldDetGroupMasters.Remove(objBestFieldDetGroupMaster);
        //        int count = pam2EntitiesContext.SaveChanges();

        //        objResult.Message = "Success";
        //        objResult.Result = true;

        //    }
        //    catch (Exception excObj)
        //    {
        //        objResult.Message = excObj.Message;
        //        objResult.Result = false;
        //        throw excObj;
        //    }
        //    return objResult;
        //}

        //public bool CheckIfFieldGroupExistsInFieldGroupSettings(string Id)
        //{
        //    bool bIsExists = false;
        //    Guid gId = new Guid(Id);
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        BestFieldDetGroupEntitywise objBestFieldDetGroupEntitywises = pam2EntitiesContext.BestFieldDetGroupEntitywises.Where(c => c.BestFieldDetGroupMasterId == gId).FirstOrDefault();
                
        //        if (objBestFieldDetGroupEntitywises != null)
        //            return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    return bIsExists;
        //}

        //public void SaveBestFieldDetPicklistFields(BestFieldDetPicklistField objBestFieldDetPicklistField)
        //{
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        pam2EntitiesContext.BestFieldDetPicklistFields.Add(objBestFieldDetPicklistField);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public void SaveBestFieldDetPicklistFieldDetail(BestFieldDetPicklistFieldDetail objBestFieldDetPicklistFieldDetail)
        {
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                pam2EntitiesContext.BestFieldDetPicklistFieldDetails.Add(objBestFieldDetPicklistFieldDetail);
                pam2EntitiesContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public void DeleteBestFieldDetPicklistFields(List<Guid> lstBestFieldDetPicklistFieldIDs)
        //{
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        List<BestFieldDetPicklistField> lstBestFieldDetPicklistField = pam2EntitiesContext.BestFieldDetPicklistFields.Where(C => lstBestFieldDetPicklistFieldIDs.Contains(C.Id)).ToList<BestFieldDetPicklistField>();

        //        pam2EntitiesContext.BestFieldDetPicklistFields.RemoveRange(lstBestFieldDetPicklistField);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void DeleteBestFieldDetPicklistFieldDetailByPicklistFieldID(List<Guid?> lstBestFieldDetPicklistFieldIDs)
        //{
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        List<BestFieldDetPicklistFieldDetail> lstBestFieldDetPicklistFieldDetail = pam2EntitiesContext.BestFieldDetPicklistFieldDetails.Where(C => lstBestFieldDetPicklistFieldIDs.Contains(C.BestFielsDetPicklistFieldsId)).ToList<BestFieldDetPicklistFieldDetail>();

        //        pam2EntitiesContext.BestFieldDetPicklistFieldDetails.RemoveRange(lstBestFieldDetPicklistFieldDetail);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public void DeleteMasterPicklistFieldandDetail(string SessionId = "0")
        {
          /*  try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<Guid> lst = null;

                if (SessionId == "0")
                {
                    lst = (from BFDSetings in pam2EntitiesContext.BestFieldDetectionSettings
                           join BFDFields in pam2EntitiesContext.BestFieldDetPicklistFields
                           on BFDSetings.BestFieldDetPicklistFieldsId equals BFDFields.Id
                           where BFDSetings.IsMaster == true
                           select BFDFields.Id).ToList<Guid>();
                }
                else
                {
                    Guid? gSessionid = new Guid(SessionId);

                    lst = (from Ses in pam2EntitiesContext.SessionBestFieldSettings
                           join
                               BFDSetings in pam2EntitiesContext.BestFieldDetectionSettings on Ses.BestFielddDetectionSettingsId equals BFDSetings.Id
                           join BFDFields in pam2EntitiesContext.BestFieldDetPicklistFields
                           on BFDSetings.BestFieldDetPicklistFieldsId equals BFDFields.Id
                           where BFDSetings.IsMaster == false && Ses.SessionId == gSessionid
                           select BFDFields.Id).ToList<Guid>();
                }
                List<Guid?> lstBestFieldDetPicklistFields = new List<Guid?>();

                foreach (Guid g in lst)
                {
                    Guid? guid = new Guid(g.ToString());
                    lstBestFieldDetPicklistFields.Add(guid);
                }

                //var lstBestFieldDetPicklistFieldDetail = (from c in pam2EntitiesContext.BestFieldDetPicklistFieldDetails
                //                                                                 join d in lstBestFieldDetPicklistFields on c.BestFielsDetPicklistFieldsId equals d.Id
                //                                          select c).AsEnumerable();

                //List<BestFieldDetPicklistFieldDetail> lstBestFieldDetPicklistFieldDetail = (from BFDSetings in pam2EntitiesContext.BestFieldDetectionSettings
                //                                                                            join BFDFields in pam2EntitiesContext.BestFieldDetPicklistFields
                //                                                                            on BFDSetings.BestFieldDetPicklistFieldsId equals BFDFields.Id
                //                                                                            join BFDFieldsDetail in pam2EntitiesContext.BestFieldDetPicklistFieldDetails
                //                                                                            on BFDFields.Id equals BFDFieldsDetail.BestFielsDetPicklistFieldsId
                //                                                                            where BFDSetings.IsMaster == true
                //                                                                            select BFDFieldsDetail).ToList<BestFieldDetPicklistFieldDetail>();

                // pam2EntitiesContext.BestFieldDetPicklistFieldDetails.Where(C=>lstBestFieldDetPicklistFields.Contains(C.BestFielsDetPicklistFieldsId)).Select(C=>C.;

                DeleteBestFieldDetPicklistFieldDetailByPicklistFieldID(lstBestFieldDetPicklistFields);
                DeleteBestFieldDetPicklistFields(lst);
            }
            catch (Exception ex)
            {
                throw ex;
            }*/
        }

        public PAMBestFieldDetectionSettingsResult GetBestFieldDetectionRulesEntitywise(string EntitySettingsId)
        {
            PAMBestFieldDetectionSettingsResult objPAMBestFieldDetectionSettingsResult = new PAMBestFieldDetectionSettingsResult();
            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettings = new List<PAMBestFieldDetectionSettings>();
            /*
            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<BestFieldDetectionRule> lstBestFieldDetectionRule = new List<BestFieldDetectionRule>();
                EntitySetting objEntitySetting = GetEntitySettingbyID(EntitySettingsId);

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account")
                {
                    lstBestFieldDetectionRule = (from c in pam2EntitiesContext.BestFieldDetectionRules
                                                 where c.Account == true
                                                 select c).ToList<BestFieldDetectionRule>();
                }

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "contact")
                {
                    lstBestFieldDetectionRule = (from c in pam2EntitiesContext.BestFieldDetectionRules
                                                 where c.Contact == true
                                                 select c).ToList<BestFieldDetectionRule>();
                }

                if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "lead")
                {
                    lstBestFieldDetectionRule = (from c in pam2EntitiesContext.BestFieldDetectionRules
                                                 where c.Lead == true
                                                 select c).ToList<BestFieldDetectionRule>();
                }

                Guid? gEntiySetting = new Guid(EntitySettingsId);
                List<BestFieldDetGroupEntitywise> lstBestFieldDetGroupEntitywise = (from c in pam2EntitiesContext.BestFieldDetGroupEntitywises
                                                                                    where c.EntitySettingId == gEntiySetting
                                                                                    select c).ToList<BestFieldDetGroupEntitywise>();

                if (lstBestFieldDetGroupEntitywise == null || lstBestFieldDetGroupEntitywise.Count == 0)
                {
                    BestFieldDetectionRule objBestFieldDetectionRule = lstBestFieldDetectionRule.Where(c => c.RuleEnum == "FieldGroup").FirstOrDefault<BestFieldDetectionRule>();
                    lstBestFieldDetectionRule.Remove(objBestFieldDetectionRule);
                }

                if (lstBestFieldDetectionRule != null)
                {
                    foreach (BestFieldDetectionRule objBestFieldDetectionRule in lstBestFieldDetectionRule)
                    {
                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettings = new PAMBestFieldDetectionSettings();
                        objPAMBestFieldDetectionSettings.Id = objBestFieldDetectionRule.Id.ToString();
                        objPAMBestFieldDetectionSettings.RuleEnum = objBestFieldDetectionRule.RuleEnum;
                        objPAMBestFieldDetectionSettings.RuleName = objBestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionRule.Account);
                        objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionRule.Contact);
                        objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionRule.Lead);

                        lstPAMBestFieldDetectionSettings.Add(objPAMBestFieldDetectionSettings);
                    }


                    objPAMBestFieldDetectionSettingsResult.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettings;
                    objPAMBestFieldDetectionSettingsResult.Result = true;
                    objPAMBestFieldDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            */
            return objPAMBestFieldDetectionSettingsResult;
        }

        public PAMBestFieldDetectionSettingsResult GetBestFieldDetectionSettingsEntitywise(string EntitySettingsId)
        {
            PAMBestFieldDetectionSettingsResult objPAMBestFieldDetectionSettingsResult = new PAMBestFieldDetectionSettingsResult();
            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettings = new List<PAMBestFieldDetectionSettings>();

            try
            {
                PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                List<BestFieldDetectionSetting> lstBestFieldDetectionSetting = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                                                where c.IsMaster == true
                                                                                orderby c.OrderOfRules ascending
                                                                                select c).ToList<BestFieldDetectionSetting>();
                EntitySetting objEntitySetting = GetEntitySettingbyID(EntitySettingsId);

              //  if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account")

                if (lstBestFieldDetectionSetting != null)
                {
                    foreach (BestFieldDetectionSetting objBestFieldDetectionSetting in lstBestFieldDetectionSetting)
                    {
                        PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettings = new PAMBestFieldDetectionSettings();

                        objPAMBestFieldDetectionSettings.Id = objBestFieldDetectionSetting.Id.ToString();

                        //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
                        //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

                        objPAMBestFieldDetectionSettings.RuleName = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
                        objPAMBestFieldDetectionSettings.RuleParamId = objBestFieldDetectionSetting.RuleParamId;

                        if (objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster != null)
                            objPAMBestFieldDetectionSettings.RuleParam = objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster.Parameter;

                     //   objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId = objBestFieldDetectionSetting.BestFieldDetGroupMasterId;
                    //    objPAMBestFieldDetectionSettings.BestFieldDetPicklistFieldsId = objBestFieldDetectionSetting.BestFieldDetPicklistFieldsId;
                     //   objPAMBestFieldDetectionSettings.GroupName = objBestFieldDetectionSetting.GroupName;
                        objPAMBestFieldDetectionSettings.RuleId = new Guid(Convert.ToString(objBestFieldDetectionSetting.RuleId));
                        objPAMBestFieldDetectionSettings.RuleEnum = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleEnum;

                    /*    if (objBestFieldDetectionSetting.BestFieldDetPicklistFieldsId != null)
                        {
                            objPAMBestFieldDetectionSettings.PickListFieldSchemaName = objBestFieldDetectionSetting.BestFieldDetPicklistField.PicklistFieldSchema;

                            objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Account);
                            objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Contact);
                            objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Lead);

                            if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account" && !objPAMBestFieldDetectionSettings.Account)
                            {
                                continue;
                            }

                            if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "contact" && !objPAMBestFieldDetectionSettings.Contact)
                            {
                                continue;
                            }

                            if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "lead" && !objPAMBestFieldDetectionSettings.Lead)
                            {
                                continue;
                            }

                            if (objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails != null)
                            {
                                List<string> list = (from c in objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails
                                                     orderby c.Order ascending
                                                     select c.Label
                                                     ).ToList<string>();

                                string Params = string.Join(", ", list.ToArray());

                                objPAMBestFieldDetectionSettings.RuleParam = Params;
                                List<HierarchyOfPickListFields> lstHierarchyOfPickListFields = new List<HierarchyOfPickListFields>();

                                foreach (var obj in objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails)
                                {
                                    HierarchyOfPickListFields objHierarchyOfPickListFields = new HierarchyOfPickListFields();
                                    objHierarchyOfPickListFields.Label = obj.Label;
                                    objHierarchyOfPickListFields.Score = Convert.ToString(obj.Order);
                                    objHierarchyOfPickListFields.Value = obj.Value;
                                    lstHierarchyOfPickListFields.Add(objHierarchyOfPickListFields);
                                }

                                objPAMBestFieldDetectionSettings.HierarchyRuleRecords = lstHierarchyOfPickListFields;
                                int count = lstHierarchyOfPickListFields.Count;

                                List<PickListScore> lstPickListScore = new List<PickListScore>();
                                PickListScore objPickListScore = new PickListScore();

                                for (int i = 1; i <= count; i++)
                                {
                                    objPickListScore = new PickListScore();
                                    objPickListScore.ScoreValue = i;
                                    objPickListScore.ScoreText = i.ToString();
                                    lstPickListScore.Add(objPickListScore);
                                }

                                objPAMBestFieldDetectionSettings.PickListScoreRecords = lstPickListScore;
                            }
                        }
                        else if (objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId != null)
                        {
                            objPAMBestFieldDetectionSettings.Account = false;
                            objPAMBestFieldDetectionSettings.Contact = false;
                            objPAMBestFieldDetectionSettings.Lead = false;

                            var list = objBestFieldDetectionSetting.BestFieldDetGroupMaster.BestFieldDetGroupEntitywises;

                            var filterList = list.Where(c => c.EntitySetting.EntityLogicalName == objEntitySetting.EntityLogicalName.ToLower().Trim()).ToList();;
                            if(filterList == null || filterList.Count == 0)
                            {
                                continue;
                            }

                        }
                        else
                        {
                            objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Account);
                            objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Contact);
                            objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Lead);
                        }
                        */
                        lstPAMBestFieldDetectionSettings.Add(objPAMBestFieldDetectionSettings);
                    }

                    objPAMBestFieldDetectionSettingsResult.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettings;
                    objPAMBestFieldDetectionSettingsResult.Result = true;
                    objPAMBestFieldDetectionSettingsResult.success = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return objPAMBestFieldDetectionSettingsResult;
        }

        //public void SaveSessionBestFieldDetectionSettings(List<SessionBestFieldSetting> lstSessionBestFieldSetting)
        //{
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        pam2EntitiesContext.SessionBestFieldSettings.AddRange(lstSessionBestFieldSetting);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public PAMBestFieldDetectionSettingsResult GetBestFieldDetectionSettingsForSession(string SessionId)
        {
            PAMBestFieldDetectionSettingsResult objPAMBestFieldDetectionSettingsResult = new PAMBestFieldDetectionSettingsResult();
            List<PAMBestFieldDetectionSettings> lstPAMBestFieldDetectionSettings = new List<PAMBestFieldDetectionSettings>();


            //try
            //{
            //    PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
            //    Guid gSessionId = new Guid(SessionId);

            //    List<BestFieldDetectionSetting> lstBestFieldDetectionSetting = (from c in pam2EntitiesContext.BestFieldDetectionSettings
            //                                                                    join d in pam2EntitiesContext.SessionBestFieldSettings on c.Id equals d.BestFielddDetectionSettingsId
            //                                                                    where c.IsMaster == false && d.SessionId == gSessionId
            //                                                                    orderby c.OrderOfRules ascending
            //                                                                    select c).ToList<BestFieldDetectionSetting>();

            //    //  if (objEntitySetting.EntityLogicalName.ToLower().Trim() == "account")

            //    if (lstBestFieldDetectionSetting != null)
            //    {
            //        foreach (BestFieldDetectionSetting objBestFieldDetectionSetting in lstBestFieldDetectionSetting)
            //        {
            //            PAMBestFieldDetectionSettings objPAMBestFieldDetectionSettings = new PAMBestFieldDetectionSettings();

            //            objPAMBestFieldDetectionSettings.Id = objBestFieldDetectionSetting.Id.ToString();

            //            //   if (!String.IsNullOrEmpty(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId)))
            //            //        objPAMBestRecordDetectionSettings.EntitySettingId = new Guid(Convert.ToString(objBestRecordDetectionSetting.EntitySettingId));

            //            objPAMBestFieldDetectionSettings.RuleName = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleName;
            //            objPAMBestFieldDetectionSettings.RuleParamId = objBestFieldDetectionSetting.RuleParamId;

            //            if (objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster != null)
            //                objPAMBestFieldDetectionSettings.RuleParam = objBestFieldDetectionSetting.BestFieldsDetRuleParametersMaster.Parameter;

            //          //  objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId = objBestFieldDetectionSetting.BestFieldDetGroupMasterId;
            //          //  objPAMBestFieldDetectionSettings.BestFieldDetPicklistFieldsId = objBestFieldDetectionSetting.BestFieldDetPicklistFieldsId;
            //          //  objPAMBestFieldDetectionSettings.GroupName = objBestFieldDetectionSetting.GroupName;
            //            objPAMBestFieldDetectionSettings.RuleId = new Guid(Convert.ToString(objBestFieldDetectionSetting.RuleId));
            //            objPAMBestFieldDetectionSettings.RuleEnum = objBestFieldDetectionSetting.BestFieldDetectionRule.RuleEnum;

            //       /*     if (objBestFieldDetectionSetting.BestFieldDetPicklistFieldsId != null)
            //            {
            //                objPAMBestFieldDetectionSettings.PickListFieldSchemaName = objBestFieldDetectionSetting.BestFieldDetPicklistField.PicklistFieldSchema;

            //                objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Account);
            //                objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Contact);
            //                objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetPicklistField.Lead);

            //                if (objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails != null)
            //                {
            //                    List<string> list = (from c in objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails
            //                                         orderby c.Order ascending
            //                                         select c.Label
            //                                         ).ToList<string>();

            //                    string Params = string.Join(", ", list.ToArray());

            //                    objPAMBestFieldDetectionSettings.RuleParam = Params;
            //                    List<HierarchyOfPickListFields> lstHierarchyOfPickListFields = new List<HierarchyOfPickListFields>();

            //                    foreach (var obj in objBestFieldDetectionSetting.BestFieldDetPicklistField.BestFieldDetPicklistFieldDetails)
            //                    {
            //                        HierarchyOfPickListFields objHierarchyOfPickListFields = new HierarchyOfPickListFields();
            //                        objHierarchyOfPickListFields.Label = obj.Label;
            //                        objHierarchyOfPickListFields.Score = obj.Order.ToString();
            //                        objHierarchyOfPickListFields.Value = obj.Value;
            //                        lstHierarchyOfPickListFields.Add(objHierarchyOfPickListFields);
            //                    }

            //                    objPAMBestFieldDetectionSettings.HierarchyRuleRecords = lstHierarchyOfPickListFields;
            //                    int count = lstHierarchyOfPickListFields.Count;

            //                    List<PickListScore> lstPickListScore = new List<PickListScore>();
            //                    PickListScore objPickListScore = new PickListScore();

            //                    for (int i = 1; i <= count; i++)
            //                    {
            //                        objPickListScore = new PickListScore();
            //                        objPickListScore.ScoreValue = i;
            //                        objPickListScore.ScoreText = i.ToString();
            //                        lstPickListScore.Add(objPickListScore);
            //                    }

            //                    objPAMBestFieldDetectionSettings.PickListScoreRecords = lstPickListScore;
            //                }
            //            }
            //            else if (objPAMBestFieldDetectionSettings.BestFieldDetGroupMasterId != null)
            //            {
                         
            //            }
            //            else
            //            {
            //                objPAMBestFieldDetectionSettings.Account = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Account);
            //                objPAMBestFieldDetectionSettings.Contact = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Contact);
            //                objPAMBestFieldDetectionSettings.Lead = Convert.ToBoolean(objBestFieldDetectionSetting.BestFieldDetectionRule.Lead);
            //            }

            //            */
            //            lstPAMBestFieldDetectionSettings.Add(objPAMBestFieldDetectionSettings);
            //        }

            //        objPAMBestFieldDetectionSettingsResult.BestFieldDetectionSettings = lstPAMBestFieldDetectionSettings;
            //        objPAMBestFieldDetectionSettingsResult.Result = true;
            //        objPAMBestFieldDetectionSettingsResult.success = true;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}

            return objPAMBestFieldDetectionSettingsResult;
        }

        //public List<BestFieldDetGroupEntitywiseDetail> GetBestFieldDetGroupEntitywiseDetail(string GroupMasterID, string EntitySettingsId)
        //{
        //    List<BestFieldDetGroupEntitywiseDetail> lstBestFieldDetGroupEntitywiseDetail = new List<BestFieldDetGroupEntitywiseDetail>();

        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        Guid gEntitySettingsId = new Guid(EntitySettingsId);
        //        Guid gGroupMasterID = new Guid(GroupMasterID);

        //        lstBestFieldDetGroupEntitywiseDetail = (from c in pam2EntitiesContext.BestFieldDetGroupEntitywises
        //                                                join d in pam2EntitiesContext.BestFieldDetGroupEntitywiseDetails on c.Id equals d.BestFieldDetGroupEntitywiseId
        //                                                where c.BestFieldDetGroupMasterId == gGroupMasterID && c.EntitySettingId == gEntitySettingsId
        //                                                select d).ToList<BestFieldDetGroupEntitywiseDetail>();

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    return lstBestFieldDetGroupEntitywiseDetail;
        //}

        //public bool CheckIfFieldGroupExistsInEntitywise(string FieldGroupId)
        //{
        //    bool bIsExists = false;
        //    Guid gFieldGroupId = new Guid(FieldGroupId);
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
        //        BestFieldDetGroupEntitywise obBestFieldDetGroupEntitywises = pam2EntitiesContext.BestFieldDetGroupEntitywises.Where(c => c.BestFieldDetGroupMasterId == gFieldGroupId).FirstOrDefault();

        //        if (obBestFieldDetGroupEntitywises != null)
        //            return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //    return bIsExists;
        //}

        //public void DeleteFieldGroupEntityWise(string EntitySettingsId)
        //{
        //    try
        //    {
        //        Guid? gEntitySettingsId = new Guid(EntitySettingsId);
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        List<BestFieldDetGroupEntitywise> lstBestFieldDetGroupEntitywise = pam2EntitiesContext.BestFieldDetGroupEntitywises.Where(c => c.EntitySettingId == gEntitySettingsId).ToList<BestFieldDetGroupEntitywise>();

        //        foreach (BestFieldDetGroupEntitywise objBestFieldDetGroupEntitywise in lstBestFieldDetGroupEntitywise)
        //        {
        //            DeleteFieldGroupEntitywiseDetail(objBestFieldDetGroupEntitywise.Id.ToString());
        //        }

        //        pam2EntitiesContext.BestFieldDetGroupEntitywises.RemoveRange(lstBestFieldDetGroupEntitywise);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void DeleteFieldGroupEntitywiseDetail(string BestFieldDetGroupEntitywiseId)
        //{
        //    try
        //    {
        //        Guid? gBestFieldDetGroupEntitywiseId = new Guid(BestFieldDetGroupEntitywiseId);
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        List<BestFieldDetGroupEntitywiseDetail> lstBestFieldDetGroupEntitywiseDetails = pam2EntitiesContext.BestFieldDetGroupEntitywiseDetails.Where(c => c.BestFieldDetGroupEntitywiseId == gBestFieldDetGroupEntitywiseId).ToList<BestFieldDetGroupEntitywiseDetail>();

        //        pam2EntitiesContext.BestFieldDetGroupEntitywiseDetails.RemoveRange(lstBestFieldDetGroupEntitywiseDetails);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void AddBestFieldDetGroupEntitywise(BestFieldDetGroupEntitywise objBestFieldDetGroupEntitywise)
        //{
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        pam2EntitiesContext.BestFieldDetGroupEntitywises.Add(objBestFieldDetGroupEntitywise);
        //        pam2EntitiesContext.SaveChanges();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public void AddBestFieldDetGroupEntitywiseDetail(List<BestFieldDetGroupEntitywiseDetail> BestFieldDetGroupEntitywiseDetails)
        //{
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

        //        if (BestFieldDetGroupEntitywiseDetails.Count > 0)
        //        {
        //            pam2EntitiesContext.BestFieldDetGroupEntitywiseDetails.AddRange(BestFieldDetGroupEntitywiseDetails);
        //            pam2EntitiesContext.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public bool CheckFieldGroupEntitywiseDependency(string MasterGroupId, string EntitySettingsId)
           
        {
            bool bIsExists = false;
            Guid gGroupId = new Guid(MasterGroupId);
            Guid gEntitySettingsId = new Guid(EntitySettingsId);

            try
            {
          /*      PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);

                //   BestFieldDetectionSetting objBestFieldDetectionSetting = pam2EntitiesContext.BestFieldDetectionSettings.Where(c => c.BestFieldDetGroupMasterId == gGroupId && c.IsMaster == false).FirstOrDefault();

                BestFieldDetectionSetting objBestFieldDetectionSetting = (from c in pam2EntitiesContext.BestFieldDetectionSettings
                                                                          join
                                                                              d in pam2EntitiesContext.SessionBestFieldSettings on c.Id equals d.BestFielddDetectionSettingsId
                                                                          where d.Session.EntitySettingId == gEntitySettingsId && c.BestFieldDetGroupMasterId == gGroupId
                                                                          select c).FirstOrDefault<BestFieldDetectionSetting>();

                if (objBestFieldDetectionSetting != null)
                {
                    return true;
                }   */
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return bIsExists;
        }


        //public ResultSet SaveFieldGroupEntityWise(List<PAMBestField_FieldGroupsDetail> PAMBestField_FieldGroupsDetails)
        //{

        //}
        //public ResultSet SaveBFDHierarchyScore(HierarchyOfPickListFields BFDPicklistFieldConfigRecords, string Entity, string PicklistField)
        //{
        //    PAM2Entities o = new pam
        //}

        //public ResultSet SaveBFDHierarchyScore(HierarchyOfPickListFields BFDPicklistFieldConfigRecords, string Entity, string PicklistField)
        //{
        //    ResultSet objResult = new ResultSet();
           
        //    try
        //    {
        //        PAM2Entities pam2EntitiesContext = new PAM2Entities(sqlConnString);
                
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    return objResult;
        //}

        #endregion

    }
}
