CREATE TABLE word_list (
    Guid UUID NOT NULL PRIMARY KEY,
    Words JSONB NOT NULL
);

CREATE TABLE IF NOT EXISTS word_list_overview (
    Guid UUID PRIMARY KEY,
    CreatedByUserGuid UUID NOT NULL,
    WordListGuid UUID NOT NULL,
    IsPublic BOOLEAN NOT NULL,
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



--INSERT INTO word_list_overview (Guid, Title, Description)
--VALUES 
--    ('550e8400-e29b-41d4-a716-446655440000', 'Test Title 1', 'Test Description 1'),
--    ('550e8400-e29b-41d4-a716-446655440001', 'Test Title 2', 'Test Description 2');