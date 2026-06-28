using DmToolsApp.Data;
using DmToolsApp.Data.Entities;
using DmToolsApp.Models;
using DmToolsApp.Models.Library;

namespace DmToolsApp.Services
{
    public class SceneDataService : ISceneDataService
    {
        private readonly AppDatabase _db;

        public SceneDataService(AppDatabase db)
        {
            _db = db;
        }

        // ── Campaigns ──────────────────────────────────────────────

        public async Task<List<Campaign>> GetCampaignsAsync()
        {
            var entities = await _db.Connection.Table<CampaignEntity>().ToListAsync();
            return entities.Select(e => new Campaign { Id = e.Id, Title = e.Title }).ToList();
        }

        public async Task SaveCampaignAsync(Campaign campaign)
        {
            var entity = new CampaignEntity { Id = campaign.Id, Title = campaign.Title };
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                campaign.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteCampaignAsync(Campaign campaign)
        {
            await _db.Connection.DeleteAsync<CampaignEntity>(campaign.Id);
        }

        // ── Sessions (chapitres) ───────────────────────────────────

        public async Task<List<Session>> GetSessionsAsync(int campaignId)
        {
            var entities = await _db.Connection.Table<SessionEntity>()
                .Where(e => e.CampaignId == campaignId)
                .ToListAsync();
            return entities.Select(e => new Session
            {
                Id = e.Id,
                CampaignId = e.CampaignId,
                Title = e.Title
            }).ToList();
        }

        public async Task SaveSessionAsync(Session session)
        {
            var entity = new SessionEntity
            {
                Id = session.Id,
                CampaignId = session.CampaignId,
                Title = session.Title
            };
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                session.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteSessionAsync(Session session)
        {
            await _db.Connection.DeleteAsync<SessionEntity>(session.Id);
        }

        // ── Scenes ────────────────────────────────────────────────

        public async Task<List<Scene>> GetScenesAsync(int sessionId)
        {
            var entities = await _db.Connection.Table<SceneEntity>()
                .Where(e => e.SessionId == sessionId)
                .ToListAsync();
            return entities.Select(e => new Scene
            {
                Id = e.Id,
                SessionId = e.SessionId,
                Title = e.Title
            }).ToList();
        }

        public async Task SaveSceneAsync(Scene scene)
        {
            var entity = new SceneEntity
            {
                Id = scene.Id,
                SessionId = scene.SessionId,
                Title = scene.Title
            };
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                scene.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteSceneAsync(Scene scene)
        {
            await _db.Connection.DeleteAsync<SceneEntity>(scene.Id);
        }

        // ── SceneTracks ───────────────────────────────────────────

        public async Task<List<SceneTrack>> GetSceneTracksAsync(int sceneId)
        {
            var sceneTrackEntities = await _db.Connection.Table<SceneTrackEntity>()
                .Where(e => e.SceneId == sceneId)
                .OrderBy(e => e.Position)
                .ToListAsync();

            var result = new List<SceneTrack>();

            foreach (var ste in sceneTrackEntities)
            {
                var trackEntity = await _db.Connection.Table<TrackEntity>()
                    .Where(t => t.Id == ste.TrackId)
                    .FirstOrDefaultAsync();

                if (trackEntity == null) continue;

                result.Add(new SceneTrack
                {
                    Id = ste.Id,
                    SceneId = ste.SceneId,
                    Position = ste.Position,
                    Volume = ste.Volume,
                    AutoPlay = ste.AutoPlay,
                    Track = new Track
                    {
                        Id = trackEntity.Id,
                        Title = trackEntity.Title,
                        FilePath = trackEntity.FilePath,
                        ImagePath = trackEntity.ImagePath,
                        Duration = trackEntity.Duration,
                        Volume = ste.Volume
                    }
                });
            }

            return result;
        }

        public async Task SaveSceneTrackAsync(SceneTrack sceneTrack)
        {
            var entity = new SceneTrackEntity
            {
                Id = sceneTrack.Id,
                SceneId = sceneTrack.SceneId,
                TrackId = sceneTrack.Track.Id,
                Volume = sceneTrack.Volume,
                Position = sceneTrack.Position,
                AutoPlay = sceneTrack.AutoPlay
            };
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                sceneTrack.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteSceneTrackAsync(SceneTrack sceneTrack)
        {
            await _db.Connection.DeleteAsync<SceneTrackEntity>(sceneTrack.Id);
        }

        public async Task UpdateSceneTrackVolumeAsync(int sceneTrackId, float volume)
        {
            var entity = await _db.Connection.FindAsync<SceneTrackEntity>(sceneTrackId);
            if (entity == null) return;
            entity.Volume = volume;
            await _db.Connection.UpdateAsync(entity);
        }
    }

    public interface ISceneDataService
    {
        Task<List<Campaign>> GetCampaignsAsync();
        Task SaveCampaignAsync(Campaign campaign);
        Task DeleteCampaignAsync(Campaign campaign);

        Task<List<Session>> GetSessionsAsync(int campaignId);
        Task SaveSessionAsync(Session session);
        Task DeleteSessionAsync(Session session);

        Task<List<Scene>> GetScenesAsync(int sessionId);
        Task SaveSceneAsync(Scene scene);
        Task DeleteSceneAsync(Scene scene);

        Task<List<SceneTrack>> GetSceneTracksAsync(int sceneId);
        Task SaveSceneTrackAsync(SceneTrack sceneTrack);
        Task DeleteSceneTrackAsync(SceneTrack sceneTrack);
        Task UpdateSceneTrackVolumeAsync(int sceneTrackId, float volume);
    }
}
