# Database Schema for Art Museum

## Relational Database Schema

### Artists Table
```markdown
| Column Name | Data Type | Constraints |
|-------------|-----------|-------------|
| artist_id | INT | PRIMARY KEY, AUTO_INCREMENT |
| first_name | VARCHAR(100) | NOT NULL |
| last_name | VARCHAR(100) | NOT NULL |
| birth_date | DATE | NOT NULL |
| birth_place | VARCHAR(100) | NOT NULL |
| death_date | DATE | NOT NULL |
| death_place | VARCHAR(100) | NOT NULL |
```

### Artworks Table
```markdown
| Column Name | Data Type | Constraints |
|-------------|-----------|-------------|
| artwork_id | INT | PRIMARY KEY, AUTO_INCREMENT |
| artist_id | INT | FOREIGN KEY REFERENCES Artists(artist_id) |
| title | VARCHAR(255) | NOT NULL |
| creation_year | INT | |
| medium | VARCHAR(100) | NOT NULL |
| dimensions | VARCHAR(100) | |
| image_data | MEDIUMBLOB | NOT NULL |
```

### Relationships
- One Artist can have many Artworks (1:N)
- Each Artwork belongs to exactly one Artist

---

## Elasticsearch Index Mapping

### Index: `museum_artworks`

```json
{
  "mappings": {
    "properties": {
      "artwork_id": {
        "type": "keyword"
      },
      "title": {
        "type": "text"
      },
      "artist": {
        "type": "object",
        "properties": {
          "first_name": {
            "type": "text"
          },
          "last_name": {
            "type": "text"
          },
          "birth_date": {
            "type": "date"
          },
          "birth_place": {
            "type": "text"
          },
          "death_date": {
            "type": "date"
          },
          "death_place": {
            "type": "text"
          }
        }
      },
      "creation_year": {
        "type": "integer"
      },
      "medium": {
        "type": "text"
      },
      "dimensions": {
        "type": "text"
      },
      "image_data": {
        "type": "binary"
      }
    }
  }
}
```
