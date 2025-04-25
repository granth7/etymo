CREATE TABLE word_list (
    Guid UUID NOT NULL PRIMARY KEY,
    CreatorGuid UUID NOT NULL,
    Words JSONB NOT NULL,
    IsPublic BOOLEAN NOT NULL
);

CREATE TABLE IF NOT EXISTS word_list_overview (
    Guid UUID PRIMARY KEY,
    CreatorGuid UUID NOT NULL,
    WordListGuid UUID NOT NULL,
    IsPublic BOOLEAN NOT NULL,
    IsHidden BOOLEAN NOT NULL DEFAULT FALSE,
    Upvotes INT NOT NULL,
    Title VARCHAR(60) NOT NULL,
    Description VARCHAR(180),
    Tags Text[],
    WordSample JSONB NOT NULL,
    CreatedDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastModifiedDate TIMESTAMP NOT NULL,
    CONSTRAINT FK_word_list_overview_word_list FOREIGN KEY (WordListGuid)
        REFERENCES word_list(Guid) ON DELETE CASCADE                               
);

CREATE TABLE user_upvotes (
    id SERIAL PRIMARY KEY,
    user_guid UUID NOT NULL,
    word_list_overview_guid UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_guid, word_list_overview_guid)
);

CREATE TABLE reports (
    Id SERIAL PRIMARY KEY,
    ReportedContentId UUID NOT NULL,
    ReporterUserId VARCHAR(255) NOT NULL,
    Reason VARCHAR(255) NOT NULL,
    Details TEXT,
    Status VARCHAR(50) DEFAULT 'pending',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ResolvedAt TIMESTAMP,
    ResolverUserId VARCHAR(255)
);

-- Add indexes for performance
CREATE INDEX idx_user_upvotes_user_guid ON user_upvotes(user_guid);
CREATE INDEX idx_user_upvotes_word_list_overview_guid ON user_upvotes(word_list_overview_guid);