#include <iostream>
#include <fstream>
#include <vector>

#pragma pack(push,1) // Выравнивание структуры
struct BITMAPFILEHEADER {
    uint16_t bfType;      // Тип файла (BM)
    uint32_t bfSize;      // Размер файла в байтах
    uint16_t bfReserved1; // Зарезервировано
    uint16_t bfReserved2; // Зарезервировано
    uint32_t bfOffBits;   // Смещение до начала данных
};
struct BITMAPINFOHEADER {
    uint32_t biSize;          // Размер этого заголовка
    int32_t  biWidth;         // Ширина изображения
    int32_t  biHeight;        // Высота изображения
    uint16_t biPlanes;        // Количество цветовых плоскостей (обычно 1)
    uint16_t biBitCount;      // Количество бит на пиксель
    uint32_t biCompression;    // Тип сжатия (0 = непостроенный)
    uint32_t biSizeImage;     // Размер изображения в байтах
    int32_t  biXPelsPerMeter;  // Горизонтальное разрешение
    int32_t  biYPelsPerMeter;  // Вертикальное разрешение
    uint32_t biClrUsed;       // Количество используемых цветов
    uint32_t biClrImportant;   // Количество важных цветов
};
#pragma pack(pop)

struct Pixel {
    unsigned char r, g, b;
};

using Segment = std::pair<std::pair<int, int>, std::pair<int, int>>;
void main_func(const std::string& filename, const std::string& output);
int countObjects(const std::vector<Segment>& segments);

int main()
{
    setlocale(LC_ALL,"Rus");
    main_func("input2.bmp", "output.bmp");
    return 0;
}

// Функция для бинаризации изображения
std::vector<std::vector<int>> binarizeBMP(const std::vector<std::vector<Pixel>>& image,
    const std::vector<Pixel>& background, int width, int height, int threshold) {

    std::vector<std::vector<int>> binaryImage(height, std::vector<int>(width, 0));

    for (int y = 0; y < height; ++y) {
        for (int x = 0; x < width; ++x) {
            if (abs(image[y][x].r - background[x].r) >= threshold) {
                binaryImage[y][x] = 1; // Пиксель объекта
            }
            else {
                if (abs(image[y][x].g - background[x].g) >= threshold) {
                    binaryImage[y][x] = 1;
                }
                else {
                    if (abs(image[y][x].b - background[x].b) >= threshold) {
                        binaryImage[y][x] = 1;
                    }
                    else {
                        binaryImage[y][x] = 0; // Фоновый пиксель
                    }
                }
            }
        }
    }

    //for (int y = 0; y < height; ++y) {
    //    for (int x = 0; x < width; ++x) {
    //        if (binaryImage[y][x] == 1) {
    //            binaryImage[y][x] = 1; // Пиксель объекта
    //        }
    //        else {
    //            binaryImage[y][x] = 0; // Фоновый пиксель
    //        }
    //    }
    //}

    return binaryImage;
}

void main_func(const std::string& filename, const std::string& output) {
    std::ifstream inputFile(filename, std::ios::binary);
    if (!inputFile) {
        std::cerr << "Не удалось открыть файл!" << std::endl;
        return;
    }

    BITMAPFILEHEADER fileHeader;
    BITMAPINFOHEADER infoHeader;

    inputFile.read(reinterpret_cast<char*>(&fileHeader), sizeof(fileHeader));
    inputFile.read(reinterpret_cast<char*>(&infoHeader), sizeof(infoHeader));

    const int width = infoHeader.biWidth;
    const int height = infoHeader.biHeight;

    std::vector<std::vector<Pixel>> image(height, std::vector<Pixel>(width));
    std::vector<Pixel> background(width);

    // Чтение пикселей изображения
    for (int y = 0; y < height; ++y) {
        for (int x = 0; x < width; ++x) {
            inputFile.read(reinterpret_cast<char*>(&image[y][x]), sizeof(Pixel));
        }
    }

    // Инициализация фона
    for (int x = 0; x < width; ++x) {
        background[x] = image[0][x]; // Используем первую строку как фон
    }

    // Примерный порог
    int threshold = 55;

    // Бинаризация изображения
    std::vector<std::vector<int>> binaryImage = binarizeBMP(image, background, width, height, threshold);

    // Создаем новый файл для записи изображения в градациях серого
    std::ofstream outputFile(output, std::ios::binary);
    outputFile.write(reinterpret_cast<char*>(&fileHeader), sizeof(fileHeader));
    outputFile.write(reinterpret_cast<char*>(&infoHeader), sizeof(infoHeader));

    // Выравнивание строки до кратности 4 байтам
    int rowSize = (infoHeader.biBitCount * infoHeader.biWidth + 31) / 32 * 4;
    std::vector<unsigned char> row(rowSize, 0); // Строка для записи

    for (int y = 0; y < infoHeader.biHeight; ++y) {
        for (int x = 0; x < infoHeader.biWidth; ++x) {
            // Бинарное значение пикселя: 0 или 1
            row[x] = binaryImage[y][x] ? 255 : 0; // Черный (255) или белый (0)
        }
        outputFile.write(reinterpret_cast<const char*>(row.data()), rowSize);
    }
    outputFile.close();
    std::cout << "бинаризация закончена" << std::endl;

    std::vector<std::pair<std::pair<int, int>, std::pair<int, int>>> pairs;

    int minLength = 3;


    for (int y = 0; y < binaryImage.size(); ++y) {
        std::pair<int, int> left{ -1, -1 };
        std::pair<int, int> right{ -1, -1 };
        bool isSegment = false;

        for (int x = 0; x < binaryImage[y].size(); ++x) {
            if (binaryImage[y][x] == 1) {
                if (!isSegment) { // Начало нового сегмента
                    left = { y, x };
                    isSegment = true;
                }
                right = { y, x }; // Обновляем правую границу сегмента
            }
            else { // Если встречаем `0`
                if (isSegment) { // Если сегмент завершен
                    if ((right.second - left.second) > minLength) {
                        pairs.push_back({ left, right });
                    }
                    isSegment = false; // Сегмент завершен
                }
            }
        }

        // Проверка для случая, если сегмент заканчивается на последнем элементе строки
        if (isSegment && (right.second - left.second) > minLength) {
            pairs.push_back({ left, right });
        }
    }

    //for (int y = 0; y < binaryImage.size() - 1; ++y) {
    //    int flag = 0;
    //    std::pair<int, int> left, right;
    //    for (int x = 0; x < binaryImage[y].size() - 1; ++x) {
    //        if (binaryImage[y][x] == 1 && flag == 1) {
    //            right.first = y;
    //            right.second = x;
    //            if(binaryImage[y][x+1] != 1)
    //                flag += 1;
    //        }
    //        if (binaryImage[y][x] == 1 && flag == 0) {
    //            left.first = y;
    //            left.second = x;
    //            flag += 1;
    //        }
    //        if (flag == 2) {
    //            std::pair<std::pair<int, int>, std::pair<int, int>> result;
    //            result.first = left; result.second = right;
    //            if((right.second - left.second) > minLength)
    //                pairs.push_back(result);
    //            flag = 0;
    //        }
    //    }
    //}

    int object_count = countObjects(pairs);
    std::cout << "Object: " << object_count;
}

// Проверка, что сегменты находятся на смежных строках (по вертикали) и пересекаются по горизонтали
bool segmentsAreConnected(const Segment& upper, const Segment& lower) {
    return (lower.first.first == upper.first.first + 1) && // Нижний отрезок на строку ниже верхнего
        (lower.second.second >= upper.first.second && lower.first.second <= upper.second.second); // Горизонтальное пересечение
}
// Рекурсивная функция для сбора всех связанных сегментов объекта
void gatherObjectSegments(int index, const std::vector<Segment>& segments, std::vector<bool>& visited, std::vector<int>& objectSegments) {
    visited[index] = true;
    objectSegments.push_back(index);

    for (size_t i = 0; i < segments.size(); ++i) {
        if (!visited[i] && segmentsAreConnected(segments[index], segments[i])) {
            gatherObjectSegments(i, segments, visited, objectSegments);
        }
    }
}

// Основная функция для подсчета объектов
int countObjects(const std::vector<Segment>& segments) {
    int objectCount = 0;
    std::vector<bool> visited(segments.size(), false);

    for (size_t i = 0; i < segments.size(); ++i) {
        if (!visited[i]) {
            // Новый объект найден; собираем все сегменты, относящиеся к этому объекту
            std::vector<int> objectSegments;
            gatherObjectSegments(i, segments, visited, objectSegments);

            objectCount++; // Завершение обработки объекта
        }
    }
    return objectCount;
}
