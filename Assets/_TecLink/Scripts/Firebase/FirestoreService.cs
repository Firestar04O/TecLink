using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TecLink.Core;
using TecLink.Models;
using UnityEngine;
using TecLinkUserProfile = TecLink.Models.UserProfile;

namespace TecLink.FirebaseIntegration
{
    [DefaultExecutionOrder(-8400)]
    [DisallowMultipleComponent]
    public sealed class FirestoreService : MonoBehaviour
    {
        private readonly TaskCompletionSource<bool> readinessSource =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private FirebaseFirestore database;

        public static FirestoreService Instance { get; private set; }

        public Task<bool> ReadinessTask => readinessSource.Task;

        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            FirebaseBootstrap.InitializationTask.ContinueWithOnMainThread(HandleFirebaseReady);
        }

        public Task<ProfileOperationResult> LoadCurrentUserProfileAsync()
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProfileOperationResult.Fail(error));
            }

            return LoadUserProfileAsync(user);
        }

        public Task<ProfileOperationResult> LoadUserProfileAsync(FirebaseUser user)
        {
            if (!IsReady || database == null)
            {
                return Task.FromResult(ProfileOperationResult.Fail(
                    "Cloud Firestore todavía no está disponible."));
            }

            if (!IsValidVerifiedUser(user, out string error))
            {
                return Task.FromResult(ProfileOperationResult.Fail(error));
            }

            var source = new TaskCompletionSource<ProfileOperationResult>();
            DocumentReference reference = database.Collection("users").Document(user.UserId);

            reference.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (!TryGetTaskResult(task, out DocumentSnapshot snapshot, out string taskError))
                {
                    source.TrySetResult(ProfileOperationResult.Fail(taskError));
                    return;
                }

                if (!snapshot.Exists)
                {
                    source.TrySetResult(ProfileOperationResult.NotFound());
                    return;
                }

                try
                {
                    TecLinkUserProfile profile = snapshot.ConvertTo<TecLinkUserProfile>();
                    if (profile == null || profile.Uid != user.UserId ||
                        !string.Equals(profile.Email, AppConstants.NormalizeEmail(user.Email),
                            StringComparison.Ordinal))
                    {
                        source.TrySetResult(ProfileOperationResult.Fail(
                            "El perfil guardado no coincide con la sesión actual."));
                        return;
                    }

                    profile.Skills = profile.Skills ?? new List<string>();
                    source.TrySetResult(ProfileOperationResult.Found(profile));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[TecLink] No se pudo convertir users/{user.UserId}: {exception}");
                    source.TrySetResult(ProfileOperationResult.Fail(
                        "El perfil guardado tiene un formato no válido."));
                }
            });

            return source.Task;
        }

        public Task<ProfileOperationResult> SaveCurrentUserProfileAsync(TecLinkUserProfile profile)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProfileOperationResult.Fail(error));
            }

            if (!ValidateProfile(profile, user, out error))
            {
                return Task.FromResult(ProfileOperationResult.Fail(error));
            }

            var source = new TaskCompletionSource<ProfileOperationResult>();
            DocumentReference reference = database.Collection("users").Document(user.UserId);

            reference.GetSnapshotAsync().ContinueWithOnMainThread(snapshotTask =>
            {
                if (!TryGetTaskResult(snapshotTask, out DocumentSnapshot snapshot, out string snapshotError))
                {
                    source.TrySetResult(ProfileOperationResult.Fail(snapshotError));
                    return;
                }

                var data = new Dictionary<string, object>
                {
                    ["uid"] = user.UserId,
                    ["email"] = AppConstants.NormalizeEmail(user.Email),
                    ["displayName"] = profile.DisplayName.Trim(),
                    ["career"] = profile.Career.Trim(),
                    ["campus"] = profile.Campus.Trim(),
                    ["bio"] = profile.Bio.Trim(),
                    ["skills"] = profile.Skills.Select(skill => skill.Trim()).ToList(),
                    ["portfolioUrl"] = profile.PortfolioUrl.Trim(),
                    ["avatarId"] = profile.AvatarId,
                    ["updatedAt"] = FieldValue.ServerTimestamp
                };

                if (!snapshot.Exists)
                {
                    data["createdAt"] = FieldValue.ServerTimestamp;
                }

                reference.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(writeTask =>
                {
                    if (!TryGetTaskResult(writeTask, out string writeError))
                    {
                        source.TrySetResult(ProfileOperationResult.Fail(writeError));
                        return;
                    }

                    LoadUserProfileAsync(user).ContinueWithOnMainThread(loadTask =>
                    {
                        if (loadTask.IsCanceled || loadTask.IsFaulted)
                        {
                            source.TrySetResult(ProfileOperationResult.Fail(
                                "El perfil se guardó, pero no pudo recargarse."));
                            return;
                        }

                        Debug.Log($"[TecLink] Perfil guardado en users/{user.UserId}.");
                        source.TrySetResult(loadTask.Result);
                    });
                });
            });

            return source.Task;
        }

        public Task<ProjectOperationResult> CreateProjectAsync(ProjectData project)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProjectOperationResult.Fail(error));
            }

            TecLinkUserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null || profile.Uid != user.UserId)
            {
                return Task.FromResult(ProjectOperationResult.Fail(
                    "Debes completar tu perfil antes de publicar un proyecto."));
            }

            if (!ValidateProject(project, out error))
            {
                return Task.FromResult(ProjectOperationResult.Fail(error));
            }

            DocumentReference reference = database.Collection("projects").Document();
            Dictionary<string, object> data = BuildProjectWriteData(
                project, user, profile, AppConstants.ProjectStatusOpen, true);

            var source = new TaskCompletionSource<ProjectOperationResult>();
            reference.SetAsync(data).ContinueWithOnMainThread(task =>
            {
                if (!TryGetTaskResult(task, out string taskError))
                {
                    source.TrySetResult(ProjectOperationResult.Fail(taskError));
                    return;
                }

                LoadProjectAsync(reference.Id).ContinueWithOnMainThread(loadTask =>
                {
                    if (loadTask.IsCanceled || loadTask.IsFaulted || !loadTask.Result.Success)
                    {
                        source.TrySetResult(ProjectOperationResult.Fail(
                            loadTask.IsCompleted && !loadTask.IsFaulted && !loadTask.IsCanceled
                                ? loadTask.Result.ErrorMessage
                                : "El proyecto se publicó, pero no pudo recargarse."));
                        return;
                    }

                    Debug.Log($"[TecLink] Proyecto creado en projects/{reference.Id}.");
                    source.TrySetResult(loadTask.Result);
                });
            });

            return source.Task;
        }

        public Task<ProjectQueryResult> LoadOpenProjectsAsync()
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProjectQueryResult.Fail(error));
            }

            Query query = database.Collection("projects")
                .WhereEqualTo("status", AppConstants.ProjectStatusOpen)
                .Limit(AppConstants.ProjectQueryLimit);
            return ExecuteProjectQueryAsync(query);
        }

        public Task<ProjectQueryResult> LoadOwnedProjectsAsync()
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProjectQueryResult.Fail(error));
            }

            Query query = database.Collection("projects")
                .WhereEqualTo("ownerId", user.UserId)
                .Limit(AppConstants.ProjectQueryLimit);
            return ExecuteProjectQueryAsync(query);
        }

        public Task<ProjectOperationResult> LoadProjectAsync(string projectId)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProjectOperationResult.Fail(error));
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Task.FromResult(ProjectOperationResult.Fail("El proyecto no tiene un ID válido."));
            }

            var source = new TaskCompletionSource<ProjectOperationResult>();
            database.Collection("projects").Document(projectId).GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (!TryGetTaskResult(task, out DocumentSnapshot snapshot, out string taskError))
                    {
                        source.TrySetResult(ProjectOperationResult.Fail(taskError));
                        return;
                    }

                    if (!snapshot.Exists)
                    {
                        source.TrySetResult(ProjectOperationResult.Fail(
                            "La publicación ya no existe."));
                        return;
                    }

                    if (!TryConvertProject(snapshot, out ProjectRecord record, out string convertError))
                    {
                        source.TrySetResult(ProjectOperationResult.Fail(convertError));
                        return;
                    }

                    source.TrySetResult(ProjectOperationResult.Saved(record));
                });
            return source.Task;
        }

        public Task<ProjectOperationResult> UpdateOwnedProjectAsync(
            string projectId,
            ProjectData project)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProjectOperationResult.Fail(error));
            }

            TecLinkUserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null || profile.Uid != user.UserId)
            {
                return Task.FromResult(ProjectOperationResult.Fail(
                    "Debes recuperar tu perfil antes de editar un proyecto."));
            }

            if (string.IsNullOrWhiteSpace(projectId) || !ValidateProject(project, out error))
            {
                return Task.FromResult(ProjectOperationResult.Fail(
                    string.IsNullOrWhiteSpace(error) ? "El proyecto no es válido." : error));
            }

            var source = new TaskCompletionSource<ProjectOperationResult>();
            DocumentReference reference = database.Collection("projects").Document(projectId);
            reference.GetSnapshotAsync().ContinueWithOnMainThread(snapshotTask =>
            {
                if (!TryGetTaskResult(snapshotTask, out DocumentSnapshot snapshot, out string snapshotError))
                {
                    source.TrySetResult(ProjectOperationResult.Fail(snapshotError));
                    return;
                }

                if (!snapshot.Exists ||
                    !snapshot.TryGetValue("ownerId", out string ownerId) ||
                    ownerId != user.UserId)
                {
                    source.TrySetResult(ProjectOperationResult.Fail(
                        "Solo el propietario puede editar esta publicación."));
                    return;
                }

                string currentStatus = snapshot.TryGetValue("status", out string storedStatus)
                    ? storedStatus
                    : AppConstants.ProjectStatusOpen;
                if (currentStatus == AppConstants.ProjectStatusClosed)
                {
                    source.TrySetResult(ProjectOperationResult.Fail(
                        "Una publicación cerrada ya no puede editarse."));
                    return;
                }

                Dictionary<string, object> data = BuildProjectWriteData(
                    project, user, profile, currentStatus, false);
                data["deadline"] = project.Deadline.HasValue
                    ? (object)project.Deadline.Value
                    : FieldValue.Delete;

                reference.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(writeTask =>
                {
                    if (!TryGetTaskResult(writeTask, out string writeError))
                    {
                        source.TrySetResult(ProjectOperationResult.Fail(writeError));
                        return;
                    }

                    LoadProjectAsync(projectId).ContinueWithOnMainThread(loadTask =>
                    {
                        if (loadTask.IsCanceled || loadTask.IsFaulted)
                        {
                            source.TrySetResult(ProjectOperationResult.Fail(
                                "El proyecto se actualizó, pero no pudo recargarse."));
                            return;
                        }

                        Debug.Log($"[TecLink] Proyecto actualizado en projects/{projectId}.");
                        source.TrySetResult(loadTask.Result);
                    });
                });
            });
            return source.Task;
        }

        public Task<ProjectOperationResult> CloseOwnedProjectAsync(string projectId)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ProjectOperationResult.Fail(error));
            }

            TecLinkUserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null || profile.Uid != user.UserId)
            {
                return Task.FromResult(ProjectOperationResult.Fail(
                    "No se pudo comprobar el perfil propietario."));
            }

            var source = new TaskCompletionSource<ProjectOperationResult>();
            DocumentReference reference = database.Collection("projects").Document(projectId);
            reference.GetSnapshotAsync().ContinueWithOnMainThread(snapshotTask =>
            {
                if (!TryGetTaskResult(snapshotTask, out DocumentSnapshot snapshot, out string snapshotError))
                {
                    source.TrySetResult(ProjectOperationResult.Fail(snapshotError));
                    return;
                }

                if (!snapshot.Exists ||
                    !snapshot.TryGetValue("ownerId", out string ownerId) ||
                    ownerId != user.UserId)
                {
                    source.TrySetResult(ProjectOperationResult.Fail(
                        "Solo el propietario puede cerrar esta publicación."));
                    return;
                }

                var updates = new Dictionary<string, object>
                {
                    ["ownerName"] = profile.DisplayName.Trim(),
                    ["ownerCareer"] = profile.Career.Trim(),
                    ["status"] = AppConstants.ProjectStatusClosed,
                    ["updatedAt"] = FieldValue.ServerTimestamp
                };
                reference.UpdateAsync(updates).ContinueWithOnMainThread(updateTask =>
                {
                    if (!TryGetTaskResult(updateTask, out string updateError))
                    {
                        source.TrySetResult(ProjectOperationResult.Fail(updateError));
                        return;
                    }

                    LoadProjectAsync(projectId).ContinueWithOnMainThread(loadTask =>
                    {
                        if (loadTask.IsCanceled || loadTask.IsFaulted)
                        {
                            source.TrySetResult(ProjectOperationResult.Fail(
                                "El proyecto se cerró, pero no pudo recargarse."));
                            return;
                        }

                        Debug.Log($"[TecLink] Proyecto cerrado en projects/{projectId}.");
                        source.TrySetResult(loadTask.Result);
                    });
                });
            });
            return source.Task;
        }

        public Task<InterestOperationResult> CreateInterestAsync(
            string projectId,
            string message)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(InterestOperationResult.Fail(error));
            }

            TecLinkUserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null || profile.Uid != user.UserId)
            {
                return Task.FromResult(InterestOperationResult.Fail(
                    "Debes completar tu perfil antes de postular."));
            }

            string cleanMessage = (message ?? string.Empty).Trim();
            if (cleanMessage.Length < AppConstants.MinimumInterestMessageLength ||
                cleanMessage.Length > AppConstants.MaximumInterestMessageLength)
            {
                return Task.FromResult(InterestOperationResult.Fail(
                    $"La presentación debe tener entre {AppConstants.MinimumInterestMessageLength} y " +
                    $"{AppConstants.MaximumInterestMessageLength} caracteres."));
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Task.FromResult(InterestOperationResult.Fail(
                    "La publicación seleccionada no es válida."));
            }

            var source = new TaskCompletionSource<InterestOperationResult>();
            DocumentReference projectReference = database.Collection("projects").Document(projectId);
            DocumentReference interestReference = projectReference.Collection("interests")
                .Document(user.UserId);

            projectReference.GetSnapshotAsync().ContinueWithOnMainThread(projectTask =>
            {
                if (!TryGetTaskResult(projectTask, out DocumentSnapshot projectSnapshot,
                        out string projectError))
                {
                    source.TrySetResult(InterestOperationResult.Fail(projectError));
                    return;
                }

                string convertError = string.Empty;
                if (!projectSnapshot.Exists ||
                    !TryConvertProject(projectSnapshot, out ProjectRecord project, out convertError))
                {
                    source.TrySetResult(InterestOperationResult.Fail(
                        projectSnapshot.Exists ? convertError : "La publicación ya no existe."));
                    return;
                }

                if (project.Data.OwnerId == user.UserId)
                {
                    source.TrySetResult(InterestOperationResult.Fail(
                        "No puedes postular a tu propio proyecto."));
                    return;
                }

                if (project.Data.Status != AppConstants.ProjectStatusOpen)
                {
                    source.TrySetResult(InterestOperationResult.Fail(
                        "Esta publicación ya no acepta postulaciones."));
                    return;
                }

                interestReference.GetSnapshotAsync().ContinueWithOnMainThread(existingTask =>
                {
                    if (!TryGetTaskResult(existingTask, out DocumentSnapshot existing,
                            out string existingError))
                    {
                        source.TrySetResult(InterestOperationResult.Fail(existingError));
                        return;
                    }

                    if (existing.Exists)
                    {
                        source.TrySetResult(InterestOperationResult.Fail(
                            "Ya enviaste una postulación para este proyecto."));
                        return;
                    }

                    var data = new Dictionary<string, object>
                    {
                        ["applicantId"] = user.UserId,
                        ["applicantName"] = profile.DisplayName.Trim(),
                        ["message"] = cleanMessage,
                        ["status"] = AppConstants.InterestStatusPending,
                        ["createdAt"] = FieldValue.ServerTimestamp,
                        ["updatedAt"] = FieldValue.ServerTimestamp
                    };

                    interestReference.SetAsync(data).ContinueWithOnMainThread(writeTask =>
                    {
                        if (!TryGetTaskResult(writeTask, out string writeError))
                        {
                            source.TrySetResult(InterestOperationResult.Fail(writeError));
                            return;
                        }

                        LoadCurrentUserInterestAsync(projectId).ContinueWithOnMainThread(loadTask =>
                        {
                            if (loadTask.IsCanceled || loadTask.IsFaulted || !loadTask.Result.Success)
                            {
                                source.TrySetResult(InterestOperationResult.Fail(
                                    "La postulación se envió, pero no pudo recargarse."));
                                return;
                            }

                            Debug.Log($"[TecLink] Postulación creada en " +
                                      $"projects/{projectId}/interests/{user.UserId}.");
                            source.TrySetResult(loadTask.Result);
                        });
                    });
                });
            });
            return source.Task;
        }

        public Task<InterestOperationResult> LoadCurrentUserInterestAsync(string projectId)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(InterestOperationResult.Fail(error));
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Task.FromResult(InterestOperationResult.Fail(
                    "La publicación seleccionada no es válida."));
            }

            var source = new TaskCompletionSource<InterestOperationResult>();
            database.Collection("projects").Document(projectId).Collection("interests")
                .Document(user.UserId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (!TryGetTaskResult(task, out DocumentSnapshot snapshot, out string taskError))
                    {
                        source.TrySetResult(InterestOperationResult.Fail(taskError));
                        return;
                    }

                    if (!snapshot.Exists)
                    {
                        source.TrySetResult(InterestOperationResult.NotFound());
                        return;
                    }

                    if (!TryConvertInterest(projectId, snapshot, out InterestRecord interest,
                            out string convertError))
                    {
                        source.TrySetResult(InterestOperationResult.Fail(convertError));
                        return;
                    }

                    source.TrySetResult(InterestOperationResult.Found(interest));
                });
            return source.Task;
        }

        public Task<InterestQueryResult> LoadProjectInterestsAsync(string projectId)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(InterestQueryResult.Fail(error));
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return Task.FromResult(InterestQueryResult.Fail(
                    "La publicación seleccionada no es válida."));
            }

            var source = new TaskCompletionSource<InterestQueryResult>();
            DocumentReference projectReference = database.Collection("projects").Document(projectId);
            projectReference.GetSnapshotAsync().ContinueWithOnMainThread(projectTask =>
            {
                if (!TryGetTaskResult(projectTask, out DocumentSnapshot projectSnapshot,
                        out string projectError))
                {
                    source.TrySetResult(InterestQueryResult.Fail(projectError));
                    return;
                }

                if (!projectSnapshot.Exists ||
                    !projectSnapshot.TryGetValue("ownerId", out string ownerId) ||
                    ownerId != user.UserId)
                {
                    source.TrySetResult(InterestQueryResult.Fail(
                        "Solo el propietario puede revisar las postulaciones."));
                    return;
                }

                projectReference.Collection("interests").Limit(AppConstants.InterestQueryLimit)
                    .GetSnapshotAsync().ContinueWithOnMainThread(queryTask =>
                    {
                        if (!TryGetTaskResult(queryTask, out QuerySnapshot querySnapshot,
                                out string queryError))
                        {
                            source.TrySetResult(InterestQueryResult.Fail(queryError));
                            return;
                        }

                        var interests = new List<InterestRecord>();
                        foreach (DocumentSnapshot document in querySnapshot.Documents)
                        {
                            if (TryConvertInterest(projectId, document, out InterestRecord interest,
                                    out string convertError))
                            {
                                interests.Add(interest);
                            }
                            else
                            {
                                Debug.LogWarning($"[TecLink] Postulación omitida: {convertError}");
                            }
                        }

                        interests = interests
                            .OrderByDescending(item => item.Data.CreatedAt.ToDateTime())
                            .ToList();
                        source.TrySetResult(InterestQueryResult.Found(interests));
                    });
            });
            return source.Task;
        }

        public async Task<InterestOperationResult> ReviewInterestAsync(
            string projectId,
            string applicantId,
            bool accept)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return InterestOperationResult.Fail(error);
            }

            TecLinkUserProfile profile = SessionManager.Instance?.CurrentProfile;
            if (profile == null || profile.Uid != user.UserId)
            {
                return InterestOperationResult.Fail(
                    "Debes tener un perfil cargado para revisar postulaciones.");
            }

            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(applicantId))
            {
                return InterestOperationResult.Fail("La postulación seleccionada no es válida.");
            }

            DocumentReference projectReference = database.Collection("projects").Document(projectId);
            DocumentReference interestReference = projectReference.Collection("interests")
                .Document(applicantId);
            string chatId = BuildChatId(projectId, applicantId);
            DocumentReference chatReference = database.Collection("chats").Document(chatId);

            try
            {
                DocumentSnapshot projectSnapshot = await projectReference.GetSnapshotAsync();
                DocumentSnapshot interestSnapshot = await interestReference.GetSnapshotAsync();

                string projectError = string.Empty;
                if (!projectSnapshot.Exists ||
                    !TryConvertProject(projectSnapshot, out ProjectRecord project, out projectError))
                {
                    return InterestOperationResult.Fail(
                        projectSnapshot.Exists ? projectError : "La publicación ya no existe.");
                }

                if (project.Data.OwnerId != user.UserId)
                {
                    return InterestOperationResult.Fail(
                        "Solo el propietario puede revisar esta postulación.");
                }

                string interestError = string.Empty;
                if (!interestSnapshot.Exists ||
                    !TryConvertInterest(projectId, interestSnapshot, out InterestRecord interest,
                        out interestError))
                {
                    return InterestOperationResult.Fail(
                        interestSnapshot.Exists ? interestError : "La postulación ya no existe.");
                }

                if (interest.Data.Status != AppConstants.InterestStatusPending)
                {
                    return InterestOperationResult.Fail(
                        "Esta postulación ya fue respondida.");
                }

                WriteBatch batch = database.StartBatch();
                var interestUpdates = new Dictionary<string, object>
                {
                    ["status"] = accept
                        ? AppConstants.InterestStatusAccepted
                        : AppConstants.InterestStatusRejected,
                    ["updatedAt"] = FieldValue.ServerTimestamp
                };

                if (accept)
                {
                    interestUpdates["chatId"] = chatId;
                    var chatData = new Dictionary<string, object>
                    {
                        ["projectId"] = projectId,
                        ["projectTitle"] = project.Data.Title,
                        ["ownerId"] = user.UserId,
                        ["applicantId"] = applicantId,
                        ["memberIds"] = new List<string> { user.UserId, applicantId },
                        ["lastMessage"] = string.Empty,
                        ["lastMessageAt"] = FieldValue.ServerTimestamp,
                        ["createdAt"] = FieldValue.ServerTimestamp
                    };
                    batch.Set(chatReference, chatData);

                    batch.Update(projectReference, new Dictionary<string, object>
                    {
                        ["ownerName"] = profile.DisplayName.Trim(),
                        ["ownerCareer"] = profile.Career.Trim(),
                        ["status"] = AppConstants.ProjectStatusSelecting,
                        ["updatedAt"] = FieldValue.ServerTimestamp
                    });
                }

                batch.Update(interestReference, interestUpdates);
                await batch.CommitAsync();

                DocumentSnapshot updatedSnapshot = await interestReference.GetSnapshotAsync();
                if (!TryConvertInterest(projectId, updatedSnapshot, out InterestRecord updated,
                        out string updatedError))
                {
                    return InterestOperationResult.Fail(updatedError);
                }

                Debug.Log($"[TecLink] Postulación {applicantId} marcada como " +
                          $"{updated.Data.Status} en projects/{projectId}.");
                return InterestOperationResult.Found(updated);
            }
            catch (Exception exception)
            {
                return InterestOperationResult.Fail(ToUserMessage(exception));
            }
        }

        public Task<ChatQueryResult> LoadCurrentUserChatsAsync()
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return Task.FromResult(ChatQueryResult.Fail(error));
            }

            var source = new TaskCompletionSource<ChatQueryResult>();
            database.Collection("chats")
                .WhereArrayContains("memberIds", user.UserId)
                .Limit(AppConstants.ChatQueryLimit)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (!TryGetTaskResult(task, out QuerySnapshot snapshot, out string taskError))
                    {
                        source.TrySetResult(ChatQueryResult.Fail(taskError));
                        return;
                    }

                    var chats = new List<ChatRecord>();
                    foreach (DocumentSnapshot document in snapshot.Documents)
                    {
                        if (TryConvertChat(document, out ChatRecord chat, out string convertError))
                        {
                            chats.Add(chat);
                        }
                        else
                        {
                            Debug.LogWarning($"[TecLink] Chat omitido: {convertError}");
                        }
                    }

                    chats = chats
                        .OrderByDescending(chat => chat.Data.LastMessageAt.ToDateTime())
                        .ToList();
                    source.TrySetResult(ChatQueryResult.Found(chats));
                });
            return source.Task;
        }

        public ListenerRegistration ListenToChatMessages(
            string chatId,
            Action<MessageQueryResult> onChanged)
        {
            if (onChanged == null)
            {
                return null;
            }

            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                onChanged(MessageQueryResult.Fail(error));
                return null;
            }

            ChatRecord selectedChat = SessionManager.Instance?.SelectedChat;
            if (selectedChat?.Data == null || selectedChat.Id != chatId ||
                !selectedChat.Data.MemberIds.Contains(user.UserId))
            {
                onChanged(MessageQueryResult.Fail(
                    "La conversación seleccionada no pertenece a tu cuenta."));
                return null;
            }

            try
            {
                Query query = database.Collection("chats").Document(chatId)
                    .Collection("messages")
                    .OrderBy("createdAt")
                    .LimitToLast(AppConstants.ChatMessageHistoryLimit);

                return query.Listen(snapshot =>
                {
                    if (snapshot == null)
                    {
                        onChanged(MessageQueryResult.Fail(
                            "Se perdió la conexión con la conversación."));
                        return;
                    }

                    try
                    {
                        var messages = new List<ChatMessageRecord>();
                        foreach (DocumentSnapshot document in snapshot.Documents)
                        {
                            if (TryConvertMessage(document, out ChatMessageRecord message,
                                    out string convertError))
                            {
                                messages.Add(message);
                            }
                            else
                            {
                                Debug.LogWarning($"[TecLink] Mensaje omitido: {convertError}");
                            }
                        }

                        messages = messages
                            .OrderBy(message => message.Data.CreatedAt.ToDateTime())
                            .ToList();
                        onChanged(MessageQueryResult.Found(messages));
                    }
                    catch (Exception exception)
                    {
                        onChanged(MessageQueryResult.Fail(ToUserMessage(exception)));
                    }
                });
            }
            catch (Exception exception)
            {
                onChanged(MessageQueryResult.Fail(ToUserMessage(exception)));
                return null;
            }
        }

        public async Task<ChatOperationResult> SendChatMessageAsync(string chatId, string text)
        {
            if (!TryGetVerifiedUser(out FirebaseUser user, out string error))
            {
                return ChatOperationResult.Fail(error);
            }

            string cleanText = (text ?? string.Empty).Trim();
            if (cleanText.Length < 1 || cleanText.Length > AppConstants.MaximumChatMessageLength)
            {
                return ChatOperationResult.Fail(
                    $"El mensaje debe tener entre 1 y {AppConstants.MaximumChatMessageLength} caracteres.");
            }

            ChatRecord selectedChat = SessionManager.Instance?.SelectedChat;
            if (string.IsNullOrWhiteSpace(chatId) || selectedChat?.Data == null ||
                selectedChat.Id != chatId || !selectedChat.Data.MemberIds.Contains(user.UserId))
            {
                return ChatOperationResult.Fail(
                    "La conversación seleccionada no pertenece a tu cuenta.");
            }

            DocumentReference chatReference = database.Collection("chats").Document(chatId);
            DocumentReference messageReference = chatReference.Collection("messages").Document();
            var messageData = new Dictionary<string, object>
            {
                ["senderId"] = user.UserId,
                ["text"] = cleanText,
                ["createdAt"] = FieldValue.ServerTimestamp
            };
            var chatUpdates = new Dictionary<string, object>
            {
                ["lastMessage"] = cleanText,
                ["lastMessageAt"] = FieldValue.ServerTimestamp
            };

            try
            {
                WriteBatch batch = database.StartBatch();
                batch.Set(messageReference, messageData);
                batch.Update(chatReference, chatUpdates);
                await batch.CommitAsync();
                Debug.Log($"[TecLink] Mensaje enviado a chats/{chatId}/messages/{messageReference.Id}.");
                return ChatOperationResult.Ok();
            }
            catch (Exception exception)
            {
                return ChatOperationResult.Fail(ToUserMessage(exception));
            }
        }

        private static bool TryConvertChat(
            DocumentSnapshot snapshot,
            out ChatRecord record,
            out string error)
        {
            record = null;
            try
            {
                Dictionary<string, object> values = snapshot.ToDictionary();
                if (!TryReadString(values, "projectId", out string projectId) ||
                    !TryReadString(values, "projectTitle", out string projectTitle) ||
                    !TryReadString(values, "ownerId", out string ownerId) ||
                    !TryReadString(values, "applicantId", out string applicantId) ||
                    !TryReadString(values, "lastMessage", out string lastMessage) ||
                    !TryReadTimestamp(values, "lastMessageAt", out Timestamp lastMessageAt) ||
                    !TryReadTimestamp(values, "createdAt", out Timestamp createdAt))
                {
                    error = "La conversación tiene campos incompletos o inválidos.";
                    return false;
                }

                List<string> memberIds = ReadStringList(values, "memberIds");
                if (memberIds.Count != 2 || !memberIds.Contains(ownerId) ||
                    !memberIds.Contains(applicantId))
                {
                    error = "La conversación no tiene participantes válidos.";
                    return false;
                }

                record = new ChatRecord
                {
                    Id = snapshot.Id,
                    Data = new ChatData
                    {
                        ProjectId = projectId,
                        ProjectTitle = projectTitle,
                        OwnerId = ownerId,
                        ApplicantId = applicantId,
                        MemberIds = memberIds,
                        LastMessage = lastMessage,
                        LastMessageAt = lastMessageAt,
                        CreatedAt = createdAt
                    }
                };
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TecLink] Chat no válido {snapshot.Id}: {exception}");
                error = "La conversación tiene un formato no válido.";
                return false;
            }
        }

        private static bool TryConvertMessage(
            DocumentSnapshot snapshot,
            out ChatMessageRecord record,
            out string error)
        {
            record = null;
            try
            {
                Dictionary<string, object> values = snapshot.ToDictionary();
                if (!TryReadString(values, "senderId", out string senderId) ||
                    !TryReadString(values, "text", out string text) ||
                    !TryReadTimestamp(values, "createdAt", out Timestamp createdAt))
                {
                    error = "El mensaje tiene campos incompletos o inválidos.";
                    return false;
                }

                record = new ChatMessageRecord
                {
                    Id = snapshot.Id,
                    Data = new ChatMessage
                    {
                        SenderId = senderId,
                        Text = text,
                        CreatedAt = createdAt
                    }
                };
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TecLink] Mensaje no válido {snapshot.Id}: {exception}");
                error = "El mensaje tiene un formato no válido.";
                return false;
            }
        }

        private static bool TryConvertInterest(
            string projectId,
            DocumentSnapshot snapshot,
            out InterestRecord record,
            out string error)
        {
            record = null;
            try
            {
                Dictionary<string, object> values = snapshot.ToDictionary();
                if (!TryReadString(values, "applicantId", out string applicantId) ||
                    !TryReadString(values, "applicantName", out string applicantName) ||
                    !TryReadString(values, "message", out string message) ||
                    !TryReadString(values, "status", out string status) ||
                    !TryReadTimestamp(values, "createdAt", out Timestamp createdAt) ||
                    !TryReadTimestamp(values, "updatedAt", out Timestamp updatedAt))
                {
                    error = "La postulación tiene campos incompletos o inválidos.";
                    return false;
                }

                record = new InterestRecord
                {
                    ProjectId = projectId,
                    Data = new ProjectInterest
                    {
                        ApplicantId = applicantId,
                        ApplicantName = applicantName,
                        Message = message,
                        Status = status,
                        ChatId = ReadOptionalString(values, "chatId"),
                        CreatedAt = createdAt,
                        UpdatedAt = updatedAt
                    }
                };
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TecLink] Postulación no válida {snapshot.Id}: {exception}");
                error = "La postulación tiene un formato no válido.";
                return false;
            }
        }

        private static string BuildChatId(string projectId, string applicantId)
        {
            return $"{projectId}_{applicantId}";
        }

        private Task<ProjectQueryResult> ExecuteProjectQueryAsync(Query query)
        {
            var source = new TaskCompletionSource<ProjectQueryResult>();
            query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (!TryGetTaskResult(task, out QuerySnapshot snapshot, out string taskError))
                {
                    source.TrySetResult(ProjectQueryResult.Fail(taskError));
                    return;
                }

                var projects = new List<ProjectRecord>();
                var conversionErrors = new List<string>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (!TryConvertProject(document, out ProjectRecord record, out string convertError))
                    {
                        Debug.LogWarning(
                            $"[TecLink] Se omitió projects/{document.Id}: {convertError}");
                        conversionErrors.Add(convertError);
                        continue;
                    }

                    projects.Add(record);
                }

                projects = projects
                    .OrderByDescending(record => record.Data.CreatedAt.ToDateTime())
                    .ToList();

                if (projects.Count == 0 && conversionErrors.Count > 0)
                {
                    source.TrySetResult(ProjectQueryResult.Fail(
                        "Firestore devolvió publicaciones, pero sus datos no pudieron leerse: " +
                        conversionErrors[0]));
                    return;
                }

                source.TrySetResult(ProjectQueryResult.Found(projects));
            });
            return source.Task;
        }

        private static bool TryConvertProject(
            DocumentSnapshot snapshot,
            out ProjectRecord record,
            out string error)
        {
            record = null;
            try
            {
                Dictionary<string, object> values = snapshot.ToDictionary();
                if (!TryReadString(values, "ownerId", out string ownerId) ||
                    !TryReadString(values, "ownerName", out string ownerName) ||
                    !TryReadString(values, "title", out string title) ||
                    !TryReadString(values, "description", out string description) ||
                    !TryReadString(values, "category", out string category) ||
                    !TryReadString(values, "modality", out string modality) ||
                    !TryReadString(values, "campus", out string campus) ||
                    !TryReadString(values, "status", out string status) ||
                    !TryReadInteger(values, "hoursPerWeek", out int hoursPerWeek) ||
                    !TryReadTimestamp(values, "createdAt", out Timestamp createdAt) ||
                    !TryReadTimestamp(values, "updatedAt", out Timestamp updatedAt))
                {
                    error = "faltan campos obligatorios o tienen un tipo incorrecto.";
                    return false;
                }

                var data = new ProjectData
                {
                    OwnerId = ownerId,
                    OwnerName = ownerName,
                    OwnerCareer = ReadOptionalString(values, "ownerCareer"),
                    Title = title,
                    Description = description,
                    Category = category,
                    RequiredSkills = ReadStringList(values, "requiredSkills"),
                    Tools = ReadStringList(values, "tools"),
                    Modality = modality,
                    Campus = campus,
                    HoursPerWeek = hoursPerWeek,
                    Deadline = TryReadTimestamp(values, "deadline", out Timestamp deadline)
                        ? deadline
                        : (Timestamp?)null,
                    Status = status,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                };

                if (data.RequiredSkills.Count == 0)
                {
                    error = "la lista requiredSkills está vacía o tiene un formato incorrecto.";
                    return false;
                }

                record = new ProjectRecord
                {
                    Id = snapshot.Id,
                    Data = data
                };
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TecLink] Proyecto no válido {snapshot.Id}: {exception}");
                error = "La publicación tiene un formato no válido.";
                return false;
            }
        }

        private static bool TryReadString(
            IDictionary<string, object> values,
            string key,
            out string value)
        {
            value = string.Empty;
            if (!values.TryGetValue(key, out object raw) || !(raw is string text))
            {
                return false;
            }

            value = text;
            return true;
        }

        private static string ReadOptionalString(IDictionary<string, object> values, string key)
        {
            return TryReadString(values, key, out string value) ? value : string.Empty;
        }

        private static bool TryReadInteger(
            IDictionary<string, object> values,
            string key,
            out int value)
        {
            value = 0;
            if (!values.TryGetValue(key, out object raw) || raw == null)
            {
                return false;
            }

            try
            {
                long converted = Convert.ToInt64(raw);
                if (converted < int.MinValue || converted > int.MaxValue)
                {
                    return false;
                }

                value = (int)converted;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool TryReadTimestamp(
            IDictionary<string, object> values,
            string key,
            out Timestamp value)
        {
            value = default;
            if (!values.TryGetValue(key, out object raw) || !(raw is Timestamp timestamp))
            {
                return false;
            }

            value = timestamp;
            return true;
        }

        private static List<string> ReadStringList(
            IDictionary<string, object> values,
            string key)
        {
            if (!values.TryGetValue(key, out object raw) || raw == null)
            {
                return new List<string>();
            }

            if (raw is IEnumerable<string> strings)
            {
                return strings.Where(value => value != null).ToList();
            }

            if (raw is IEnumerable<object> objects)
            {
                return objects.OfType<string>().ToList();
            }

            return new List<string>();
        }

        private static Dictionary<string, object> BuildProjectWriteData(
            ProjectData project,
            FirebaseUser user,
            TecLinkUserProfile profile,
            string status,
            bool includeCreatedAt)
        {
            var data = new Dictionary<string, object>
            {
                ["ownerId"] = user.UserId,
                ["ownerName"] = profile.DisplayName.Trim(),
                ["ownerCareer"] = profile.Career.Trim(),
                ["title"] = project.Title.Trim(),
                ["description"] = project.Description.Trim(),
                ["category"] = project.Category.Trim(),
                ["requiredSkills"] = CleanDistinctList(project.RequiredSkills),
                ["tools"] = CleanDistinctList(project.Tools),
                ["modality"] = project.Modality,
                ["campus"] = project.Campus.Trim(),
                ["hoursPerWeek"] = project.HoursPerWeek,
                ["status"] = status,
                ["updatedAt"] = FieldValue.ServerTimestamp
            };

            if (includeCreatedAt)
            {
                data["createdAt"] = FieldValue.ServerTimestamp;
            }

            if (project.Deadline.HasValue)
            {
                data["deadline"] = project.Deadline.Value;
            }

            return data;
        }

        private void HandleFirebaseReady(Task<bool> task)
        {
            if (task.IsCanceled || task.IsFaulted || !task.Result)
            {
                readinessSource.TrySetResult(false);
                return;
            }

            try
            {
                database = FirebaseFirestore.DefaultInstance;
                IsReady = true;
                readinessSource.TrySetResult(true);
                Debug.Log("[TecLink] Cloud Firestore listo.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TecLink] No se pudo iniciar Cloud Firestore: {exception}");
                readinessSource.TrySetResult(false);
            }
        }

        private bool TryGetVerifiedUser(out FirebaseUser user, out string error)
        {
            user = AuthService.Instance?.CurrentUser;
            if (!IsReady || database == null)
            {
                error = "Cloud Firestore todavía no está disponible.";
                return false;
            }

            return IsValidVerifiedUser(user, out error);
        }

        private static bool IsValidVerifiedUser(FirebaseUser user, out string error)
        {
            if (user == null)
            {
                error = "No hay una sesión iniciada.";
                return false;
            }

            if (!user.IsEmailVerified)
            {
                error = "Verifica tu correo antes de usar el perfil.";
                return false;
            }

            if (!AppConstants.IsInstitutionalEmail(user.Email))
            {
                error = "La sesión no pertenece al dominio institucional permitido.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateProfile(TecLinkUserProfile profile, FirebaseUser user, out string error)
        {
            if (profile == null)
            {
                error = "El perfil está vacío.";
                return false;
            }

            if (profile.Uid != user.UserId ||
                !string.Equals(AppConstants.NormalizeEmail(profile.Email),
                    AppConstants.NormalizeEmail(user.Email), StringComparison.Ordinal))
            {
                error = "El perfil no coincide con la sesión actual.";
                return false;
            }

            if (profile.Skills == null || profile.Skills.Count == 0 ||
                profile.Skills.Count > AppConstants.MaximumSkills)
            {
                error = $"Agrega entre 1 y {AppConstants.MaximumSkills} habilidades.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateProject(ProjectData project, out string error)
        {
            if (project == null)
            {
                error = "El proyecto está vacío.";
                return false;
            }

            string title = (project.Title ?? string.Empty).Trim();
            string description = (project.Description ?? string.Empty).Trim();
            string category = (project.Category ?? string.Empty).Trim();
            string campus = (project.Campus ?? string.Empty).Trim();

            if (title.Length < AppConstants.MinimumProjectTitleLength ||
                title.Length > AppConstants.MaximumProjectTitleLength)
            {
                error = "El título del proyecto no tiene una longitud válida.";
                return false;
            }

            if (description.Length < AppConstants.MinimumProjectDescriptionLength ||
                description.Length > AppConstants.MaximumProjectDescriptionLength)
            {
                error = "La descripción del proyecto no tiene una longitud válida.";
                return false;
            }

            if (category.Length < 2 || category.Length > AppConstants.MaximumProjectCategoryLength ||
                campus.Length < 2 || campus.Length > AppConstants.MaximumCampusLength)
            {
                error = "La categoría o la sede no son válidas.";
                return false;
            }

            if (!IsValidStringList(project.RequiredSkills, 1, AppConstants.MaximumSkills,
                    AppConstants.MaximumSkillLength) ||
                !IsValidStringList(project.Tools, 0, AppConstants.MaximumProjectTools,
                    AppConstants.MaximumProjectToolLength))
            {
                error = "Las habilidades o herramientas del proyecto no son válidas.";
                return false;
            }

            if (project.Modality != AppConstants.ProjectModalityOnSite &&
                project.Modality != AppConstants.ProjectModalityRemote &&
                project.Modality != AppConstants.ProjectModalityHybrid)
            {
                error = "Selecciona una modalidad válida.";
                return false;
            }

            if (project.HoursPerWeek < AppConstants.MinimumProjectHoursPerWeek ||
                project.HoursPerWeek > AppConstants.MaximumProjectHoursPerWeek)
            {
                error = "Las horas semanales no son válidas.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static List<string> CleanDistinctList(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Select(value => (value ?? string.Empty).Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsValidStringList(
            IEnumerable<string> values,
            int minimum,
            int maximum,
            int itemLimit)
        {
            List<string> cleanValues = CleanDistinctList(values);
            return cleanValues.Count >= minimum && cleanValues.Count <= maximum &&
                   cleanValues.All(value => value.Length <= itemLimit);
        }

        private static bool TryGetTaskResult<T>(Task<T> task, out T result, out string error)
        {
            result = default;
            if (task.IsCanceled)
            {
                error = "La operación con Firestore fue cancelada.";
                return false;
            }

            if (task.IsFaulted)
            {
                error = ToUserMessage(task.Exception);
                return false;
            }

            result = task.Result;
            error = string.Empty;
            return true;
        }

        private static bool TryGetTaskResult(Task task, out string error)
        {
            if (task.IsCanceled)
            {
                error = "La operación con Firestore fue cancelada.";
                return false;
            }

            if (task.IsFaulted)
            {
                error = ToUserMessage(task.Exception);
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string ToUserMessage(Exception exception)
        {
            FirestoreException firestoreException = FindFirestoreException(exception);
            if (firestoreException != null)
            {
                Debug.LogWarning(
                    $"[TecLink] Firestore error {firestoreException.ErrorCode}: " +
                    firestoreException.Message);
                return FirestoreErrorToUserMessage(
                    firestoreException.ErrorCode,
                    firestoreException.ErrorCode.ToString());
            }

            FirebaseException firebaseException = FindFirebaseException(exception);
            if (firebaseException == null)
            {
                Debug.LogError($"[TecLink] Error de Firestore no reconocido: {exception}");
                return "Firestore produjo un error inesperado.";
            }

            FirestoreError error = (FirestoreError)firebaseException.ErrorCode;
            Debug.LogWarning(
                $"[TecLink] Firestore error {error} ({firebaseException.ErrorCode}): " +
                firebaseException.Message);

            return FirestoreErrorToUserMessage(error, firebaseException.ErrorCode.ToString());
        }

        private static string FirestoreErrorToUserMessage(FirestoreError error, string code)
        {
            switch (error)
            {
                case FirestoreError.PermissionDenied:
                    return "Firestore rechazó la operación. Revisa que el correo esté verificado y las reglas estén publicadas.";
                case FirestoreError.Unauthenticated:
                    return "La sesión ya no es válida. Inicia sesión nuevamente.";
                case FirestoreError.Unavailable:
                    return "Firestore no está disponible. Revisa Internet e inténtalo nuevamente.";
                case FirestoreError.NotFound:
                    return "No se encontró el recurso solicitado en Firestore.";
                default:
                    return $"Firestore no pudo completar la operación. Código: {error} ({code}).";
            }
        }

        private static FirestoreException FindFirestoreException(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions
                    .Select(FindFirestoreException)
                    .FirstOrDefault(candidate => candidate != null);
            }

            if (exception is FirestoreException firestoreException)
            {
                return firestoreException;
            }

            return exception?.InnerException == null
                ? null
                : FindFirestoreException(exception.InnerException);
        }

        private static FirebaseException FindFirebaseException(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                return aggregateException.Flatten().InnerExceptions
                    .Select(FindFirebaseException)
                    .FirstOrDefault(candidate => candidate != null);
            }

            if (exception is FirebaseException firebaseException)
            {
                return firebaseException;
            }

            return exception?.InnerException == null
                ? null
                : FindFirebaseException(exception.InnerException);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
